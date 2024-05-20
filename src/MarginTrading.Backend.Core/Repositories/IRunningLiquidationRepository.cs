// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IRunningLiquidationRepository
    {
        /// <summary>
        /// Try to add running liquidation info for the account into storage
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="runningLiquidation"></param>
        /// <returns></returns>
        Task<bool> TryAdd(string accountId, RunningLiquidation runningLiquidation);
        
        /// <summary>
        /// Try to remove running liquidation info for the account from storage
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        Task<bool> TryRemove(string accountId);
        
        /// <summary>
        /// Get running liquidation info for multiple accounts
        /// </summary>
        /// <param name="accountIds"></param>
        /// <returns></returns>
        IAsyncEnumerable<RunningLiquidation> Get(string[] accountIds);
    }
}