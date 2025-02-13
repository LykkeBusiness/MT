// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;

using Common;

using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services.Mappers
{
    public static class TradingEngineSnapshotRawMappers
    {
        internal static TradingEngineSnapshot ToSnapshot(this TradingEngineSnapshotRaw source)
        {
            return new(
                source.TradingDay,
                source.CorrelationId,
                source.Timestamp,
                source.Orders.Select(o => source.Status == SnapshotStatus.Draft
                    ? (object)o
                    : o.ConvertToContract(
                        source.RelatedOrders.TryGetValue(o.Id, out var orders)
                        ? [.. orders]
                        : [])).ToJson(),
                source.Positions.Select(p => source.Status == SnapshotStatus.Draft
                    ? (object)p
                    : p.ConvertToContract(source.PositionsRelatedOrders.TryGetValue(p.Id, out var orders)
                        ? [.. orders]
                        : [])).ToJson(),
                source.Accounts.Select(a => source.Status == SnapshotStatus.Draft
                    ? (object)a
                    : a.ConvertToContract(source.AccountsInLiquidation.Contains(a))).ToJson(),
                source.BestFxPrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ConvertToContract()).ToJson(),
                source.BestTradingPrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ConvertToContract()).ToJson(),
                source.Status);
        }
    }
}