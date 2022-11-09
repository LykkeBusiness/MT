// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.AccountHistory;
using MarginTrading.Backend.Core.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MarginTrading.Backend.Services
{
    public class Bugs2826RestoreTool
    {
        private readonly IAccountHistoryRepository _accountHistoryRepository;
        private readonly OrdersCache _ordersCache;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<Bugs2826RestoreTool> _logger;

        public Bugs2826RestoreTool(IAccountHistoryRepository accountHistoryRepository,
            OrdersCache ordersCache,
            IConnectionMultiplexer redis,
            ILogger<Bugs2826RestoreTool> logger)
        {
            _accountHistoryRepository = accountHistoryRepository;
            _ordersCache = ordersCache;
            _redis = redis;
            _logger = logger;
        }
        
        public async Task Restore(DateTime day, bool demoMode)
        {
            var modeMessage = demoMode ? "in DEMO mode" : "in REAL mode";
            _logger.LogInformation("BUGS-2826 restore tool is running {Mode}", modeMessage);
            
            var changes = await _accountHistoryRepository.GetUnrealizedPnlPerPosition(day);
            
            var restoreResult = await FindRestoreResult(day) ?? new RestoreResult(RestoreStatus.NotStarted, day, new RestoreProgress(changes.Count, 0));

            if (restoreResult.Status == RestoreStatus.Finished || restoreResult.Status == RestoreStatus.Failed)
            {
                _logger.LogWarning(
                    "The BUGS-2826 restore tool has already completed for trading day {Day} with status {Status}",
                    day, restoreResult.Status.ToString());
                return;
            } else if (restoreResult.Status == RestoreStatus.NotStarted)
            {
                _logger.LogInformation("Processing positions (BUGS-2826) for trading day {Day} started", day);   
            } else if (restoreResult.Status == RestoreStatus.InProgress)
            {
                _logger.LogInformation("Proceeding with processing positions (BUGS-2826) for trading day {Day}", day);
            }

            await _lock.WaitAsync();

            try
            {
                restoreResult.Status = RestoreStatus.InProgress;
                
                foreach (var entry in changes)
                {
                    if (restoreResult.FoundPositions.ContainsKey(entry.PositionId))
                    {
                        _logger.LogInformation("Position {PositionId} has already been processed, skipping", entry.PositionId);
                        continue;
                    }
                    
                    if (restoreResult.NotFoundPositions.ContainsKey(entry.PositionId))
                    {
                        _logger.LogInformation("Position {PositionId} has already been processed as not found, skipping", entry.PositionId);
                        continue;
                    }
                    
                    if (_ordersCache.Positions.TryGetPositionById(entry.PositionId, out var position))
                    {
                        if (!demoMode)
                        {
                            position.ChargePnL(entry.OperationId, entry.ChangeAmount);
                        }

                        restoreResult.AddProcessed(entry.PositionId, entry.ChangeAmount);
                        _logger.LogInformation("Successfully processed unrealized PnL for position {PositionId} with change amount {Amount}",
                            entry.PositionId, entry.ChangeAmount);
                    }
                    else
                    {
                        restoreResult.AddNotFound(entry.PositionId, entry.ChangeAmount);
                        _logger.LogWarning("Position {PositionId} not found in cache", entry.PositionId);
                    }

                    restoreResult.Progress = new RestoreProgress(restoreResult.Progress.Total,
                        restoreResult.Progress.Processed + 1);

                    await SaveRestoreResult(restoreResult);
                }
                
                restoreResult.Status = RestoreStatus.Finished;
            }
            catch (Exception e)
            {
                restoreResult.Status = RestoreStatus.Failed;
                _logger.LogError(e, "Failed to process positions (BUGS-2826) for trading day {Day}", day);
            }
            finally
            {
                await SaveRestoreResult(restoreResult);
                _lock.Release();
            }
        }
        
        [ItemCanBeNull]
        public async Task<RestoreResult> FindRestoreResult(DateTime day)
        {
            var json = await _redis
                .GetDatabase()
                .StringGetAsync(GetRedisKey(day));

            if (json.HasValue)
            {
                var result = JsonConvert.DeserializeObject<RestoreResult>(json);
                return result;
            }

            return null;
        }
        
        public async Task<bool> RestoreCleanup(DateTime day)
        {
            var key = GetRedisKey(day);
            
            var removed = await _redis
                .GetDatabase()
                .KeyDeleteAsync(key);

            return removed;
        }

        private async Task SaveRestoreResult(RestoreResult restoreResult)
        {
            var json = JsonConvert.SerializeObject(restoreResult);

            await _redis
                .GetDatabase()
                .StringSetAsync(GetRedisKey(restoreResult.Date), json);
        }
        
        private static string GetRedisKey(DateTime day)
        {
            return $"trading-core:bugs2826:restore:result:{day:yyyy-MM-dd}";
        }
    }
}