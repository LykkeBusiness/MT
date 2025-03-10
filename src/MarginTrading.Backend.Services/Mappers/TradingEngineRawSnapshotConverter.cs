// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

using Common;

using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services.Mappers;

internal static class TradingEngineRawSnapshotConverter
{
    internal static TradingEngineSnapshot Convert(this TradingEngineSnapshotRaw source) => new(
            source.TradingDay,
            source.CorrelationId,
            source.Timestamp,
            source.ConvertOrders(),
            source.ConvertPositions(),
            source.ConvertAccounts(),
            source.BestFxPrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ConvertToContract()).ToJson(),
            source.BestTradingPrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ConvertToContract()).ToJson(),
            source.Status);

    internal static ImmutableArray<Core.Trading.Order> GetRelatedOrdersOrEmpty(this TradingEngineSnapshotRaw source, string orderId) =>
        source.RelatedOrders.TryGetValue(orderId, out var orders) ? orders : [];

    internal static ImmutableArray<Core.Trading.Order> GetPositionRelatedOrdersOrEmpty(this TradingEngineSnapshotRaw source, string positionId) =>
        source.PositionsRelatedOrders.TryGetValue(positionId, out var orders) ? orders : [];

    internal static string ConvertOrders(this TradingEngineSnapshotRaw source) =>
        source.Status switch
        {
            SnapshotStatus.Draft => source.Orders.ToJson(),
            _ => source.Orders.Select(o => o.ConvertToContract([.. source.GetRelatedOrdersOrEmpty(o.Id)])).ToJson()
        };

    internal static string ConvertPositions(this TradingEngineSnapshotRaw source) =>
        source.Status switch
        {
            SnapshotStatus.Draft => source.Positions.ToJson(),
            _ => source.Positions.Select(p => p.ConvertToContract([.. source.GetPositionRelatedOrdersOrEmpty(p.Id)])).ToJson()
        };

    internal static string ConvertAccounts(this TradingEngineSnapshotRaw source) =>
        source.Status switch
        {
            SnapshotStatus.Draft => source.Accounts.ToJson(),
            _ => source.Accounts.Select(a => a.ConvertToContract(source.AccountsInLiquidation.Contains(a))).ToJson()
        };
}