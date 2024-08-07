// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.Backend.Services
{
    internal class RunningLiquidationLogger : IRunningLiquidationRepository
    {
        private readonly IRunningLiquidationRepository _decoratee;
        private readonly ILog _logger;

        public RunningLiquidationLogger(IRunningLiquidationRepository decoratee, ILog logger)
        {
            _decoratee = decoratee;
            _logger = logger;
        }

        public async Task<bool> TryAdd(string accountId, RunningLiquidation runningLiquidation)
        {
            var result = await _decoratee.TryAdd(accountId, runningLiquidation);
            if (result)
            {
                await _logger.WriteInfoAsync(
                    nameof(RunningLiquidationLogger), 
                    nameof(TryAdd),
                    runningLiquidation.ToJson(), 
                    "Running liquidation added for account");
                return true;
            }

            await _logger.WriteWarningAsync(
                nameof(RunningLiquidationLogger), 
                nameof(TryAdd), 
                runningLiquidation.ToJson(), "Failed to add running liquidation for account");
            return false;
        }

        public async Task<bool> TryRemove(string accountId)
        {
            var result = await _decoratee.TryRemove(accountId);
            if (result)
            {
                await _logger.WriteInfoAsync(
                    nameof(RunningLiquidationLogger), 
                    nameof(TryRemove), 
                    accountId,
                    "Running liquidation removed for account");
                return true;
            }

            await _logger.WriteWarningAsync(
                nameof(RunningLiquidationLogger), 
                nameof(TryRemove), 
                accountId,
                "Failed to remove running liquidation for account");
            return false;
        }

        public async IAsyncEnumerable<RunningLiquidation> Get(string[] accountIds)
        {
            var result = await _decoratee.Get(accountIds).ToListAsync();

            try
            {
                var accountsInLiquidation = result.Select(x => x.AccountId);
                var restAccounts = accountIds.Except(accountsInLiquidation);
                foreach (var noLiquidationForAccountId in restAccounts)
                {
                    await _logger.WriteWarningAsync(
                        nameof(RunningLiquidationLogger), 
                        nameof(Get),
                        noLiquidationForAccountId,
                        "There is no running liquidation for account");
                }
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync(
                    nameof(RunningLiquidationLogger), 
                    nameof(Get),
                    new { accountIds }.ToJson(), 
                    e);
            }

            foreach (var runningLiquidation in result)
            {
                yield return runningLiquidation;
            }
        }
    }
}