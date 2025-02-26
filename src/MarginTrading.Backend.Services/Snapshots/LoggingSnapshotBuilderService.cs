// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Snapshots;

internal class LoggingSnapshotBuilderService(
    ISnapshotBuilderService decoratee,
    ILogger<LoggingSnapshotBuilderService> logger) : ISnapshotBuilderService
{
    public async Task ConvertToFinal(
        string correlationId,
        IEnumerable<ClosingAssetPrice> cfdQuotes,
        IEnumerable<ClosingFxRate> fxRates,
        IDraftSnapshotKeeper draftSnapshotKeeper = null)
    {
        await decoratee.ConvertToFinal(
            correlationId,
            cfdQuotes,
            fxRates,
            draftSnapshotKeeper);

        logger.LogInformation(
            "Snapshot was converted to final. CorrelationId: {CorrelationId}",
            correlationId);
    }

    public async Task<TradingEngineSnapshotSummary> MakeSnapshot(
        DateTime tradingDay,
        string correlationId,
        EnvironmentValidationStrategyType strategyType,
        SnapshotStatus status = SnapshotStatus.Final)
    {
        logger.LogInformation(
            "Making snapshot {Status} for {TradingDay}. CorrelationId: {CorrelationId}",
            status,
            tradingDay.ToString("yyyy-MM-dd"),
            correlationId);

        var summary = await decoratee.MakeSnapshot(
            tradingDay,
            correlationId,
            strategyType,
            status);

        logger.LogInformation(
            "Snapshot {Status} for {TradingDay} was created. CorrelationId: {CorrelationId}",
            status,
            tradingDay.ToString("yyyy-MM-dd"),
            correlationId);
        return summary;
    }
}
