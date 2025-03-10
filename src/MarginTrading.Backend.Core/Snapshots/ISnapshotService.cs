// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Snapshots;

public interface ISnapshotService
{
    /// <summary>
    /// Make final trading snapshot from current system state.
    /// Might be long running operation.
    /// </summary>
    /// <param name="tradingDay"></param>
    /// <param name="correlationId"></param>
    /// <param name="status"></param>
    /// <returns>Summary of the snapshot</returns>
    Task<TradingEngineSnapshotSummary> Make(
        DateTime tradingDay,
        string correlationId, // remove from the API, it is cross cutting concern
        EnvironmentValidationStrategyType strategyType,
        SnapshotInitiator initiator,
        SnapshotStatus status = SnapshotStatus.Final);
}