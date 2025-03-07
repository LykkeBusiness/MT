// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTradingTests.Services
{
    internal sealed class RunningLiquidationRepositoryFake : IRunningLiquidationRepository
    {
        public Task<bool> TryAdd(string accountId, RunningLiquidation runningLiquidation)
        {
            return Task.FromResult(true);
        }

        public Task<bool> TryRemove(string accountId)
        {
            return Task.FromResult(true);
        }

        public async IAsyncEnumerable<RunningLiquidation> Get(string[] accountIds)
        {
            await Task.Yield();

            foreach (var accountId in accountIds)
            {
                yield return new RunningLiquidation(accountId, Guid.NewGuid().ToString("N"));
            }
        }
    }
}