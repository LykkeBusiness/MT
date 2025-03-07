// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Snapshots;

public static class SnapshotBuilderServiceExtensions
{
    public static Task<TradingEngineSnapshotSummary> MakeSnapshot(
        this ISnapshotBuilderService service,
        SnapshotCreationRequest request) =>
        service.MakeSnapshot(
            request.TradingDay,
            request.CorrelationId,
            request.ValidationStrategyType,
            request.Initiator,
            request.Status);
}
