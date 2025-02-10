// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services
{
    public interface ISnapshotBuilder
    {
        /// <summary>
        /// Make final trading snapshot from current system state.
        /// Might be long running operation.
        /// </summary>
        /// <param name="tradingDay"></param>
        /// <param name="correlationId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        Task<string> MakeTradingDataSnapshot(
            DateTime tradingDay,
            string correlationId,
            SnapshotStatus status = SnapshotStatus.Final);

        /// <summary>
        /// Make final trading snapshot by converting draft snapshot
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="cfdQuotes"></param>
        /// <param name="fxRates"></param>
        /// <returns></returns>
        // TODO: probably we better make this feature a concern of another service
        // so far the only reason features are combined under the same service is that they are sharing lock
        Task Convert(
            string correlationId,
            IEnumerable<ClosingAssetPrice> cfdQuotes,
            IEnumerable<ClosingFxRate> fxRates,
            IDraftSnapshotKeeper draftSnapshotKeeper = null);
    }
}