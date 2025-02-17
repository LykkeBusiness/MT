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

        public IAsyncEnumerable<RunningLiquidation> Get(string[] accountIds) => _decoratee.Get(accountIds);
    }
}