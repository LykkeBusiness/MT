// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.Services;

namespace MarginTrading.Backend.Services.Snapshots;

internal class SnapshotBuilderServiceDecorator(
    ISnapshotBuilderService decoratee,
    ISnapshotBuilderDraftRebuildAgent snapshotDraftRebuildAgent) : ISnapshotBuilderService
{
    public Task ConvertToFinal(
        string correlationId,
        IEnumerable<ClosingAssetPrice> cfdQuotes,
        IEnumerable<ClosingFxRate> fxRates,
        IDraftSnapshotKeeper draftSnapshotKeeper = null) =>
        decoratee.ConvertToFinal(
            correlationId,
            cfdQuotes,
            fxRates,
            draftSnapshotKeeper);

    public async Task<TradingEngineSnapshotSummary> MakeSnapshot(
        DateTime tradingDay,
        string correlationId,
        EnvironmentValidationStrategyType strategyType,
        SnapshotInitiator initiator,
        SnapshotStatus status = SnapshotStatus.Final)
    {
        var summary = await decoratee.MakeSnapshot(
            tradingDay,
            correlationId,
            strategyType,
            initiator,
            status);
        if (status == SnapshotStatus.Draft)
            await snapshotDraftRebuildAgent.ResetDraftRebuildFlag();
        return summary;
    }
}