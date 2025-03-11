// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Snapshots;

internal sealed class LoggingSnapshotService(
    ISnapshotService decoratee,
    ILogger<LoggingSnapshotService> logger) : ISnapshotService
{
    public async Task<TradingEngineSnapshotSummary> Make(
        DateTime tradingDay,
        string correlationId,
        EnvironmentValidationStrategyType strategyType,
        SnapshotInitiator initiator,
        SnapshotStatus status = SnapshotStatus.Final)
    {
        logger.LogInformation(
            "Making snapshot {Status} for {TradingDay}. CorrelationId: {CorrelationId}, Initiator: {Initiator}",
            status,
            tradingDay.ToString("yyyy-MM-dd"),
            correlationId,
            initiator);

        var summary = await decoratee.Make(
            tradingDay,
            correlationId,
            strategyType,
            initiator,
            status);

        logger.LogInformation(
            "Snapshot {Status} for {TradingDay} was created. CorrelationId: {CorrelationId}, Initiator: {Initiator}",
            status,
            tradingDay.ToString("yyyy-MM-dd"),
            correlationId,
            initiator);
        return summary;
    }
}
