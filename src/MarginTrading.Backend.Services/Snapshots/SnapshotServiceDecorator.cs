// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.Services;

namespace MarginTrading.Backend.Services.Snapshots;

internal class SnapshotServiceDecorator(
    ISnapshotService decoratee,
    ISnapshotBuilderDraftRebuildAgent snapshotDraftRebuildAgent) : ISnapshotService
{
    public async Task<TradingEngineSnapshotSummary> Make(
        DateTime tradingDay,
        string correlationId,
        EnvironmentValidationStrategyType strategyType,
        SnapshotInitiator initiator,
        SnapshotStatus status = SnapshotStatus.Final)
    {
        var summary = await decoratee.Make(
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