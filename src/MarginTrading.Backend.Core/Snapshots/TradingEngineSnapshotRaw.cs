// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Snapshots;

public record TradingEngineSnapshotRaw(
    DateTime TradingDay,
    string CorrelationId,
    DateTime Timestamp,
    SnapshotStatus Status,
    ImmutableArray<Order> Orders,
    ImmutableDictionary<string, ImmutableArray<Order>> RelatedOrders,
    ImmutableArray<Position> Positions,
    ImmutableDictionary<string, ImmutableArray<Order>> PositionsRelatedOrders,
    ImmutableArray<MarginTradingAccount> Accounts,
    ImmutableArray<MarginTradingAccount> AccountsInLiquidation,
    ImmutableDictionary<string, InstrumentBidAskPair> BestFxPrices,
    ImmutableDictionary<string, InstrumentBidAskPair> BestTradingPrices)
{
    public static TradingEngineSnapshotRaw Empty => new(
        default,
        string.Empty,
        default,
        default,
        [],
        ImmutableDictionary<string, ImmutableArray<Order>>.Empty,
        [],
        ImmutableDictionary<string, ImmutableArray<Order>>.Empty,
        [],
        [],
        ImmutableDictionary<string, InstrumentBidAskPair>.Empty,
        ImmutableDictionary<string, InstrumentBidAskPair>.Empty);
}

public record TradingEngineSnapshotStatistics(
    DateTime TradingDay,
    int OrdersCount,
    int PositionsCount,
    int AccountsCount,
    int BestFxPricesCount,
    int BestTradingPricesCount)
{
    public static TradingEngineSnapshotStatistics Empty => new(
        default,
        default,
        default,
        default,
        default,
        default);

    public override string ToString() =>
        $"TradingDay: {TradingDay:yyyy-MM-dd}, Orders: {OrdersCount}, positions: {PositionsCount}, accounts: {AccountsCount}, best FX prices: {BestFxPricesCount}, best trading prices: {BestTradingPricesCount}.";

    public static implicit operator string(TradingEngineSnapshotStatistics statistics) => statistics.ToString();
}

public static class TradingEngineSnapshotRawExtensions
{
    public static TradingEngineSnapshotStatistics GetStatistics(this TradingEngineSnapshotRaw snapshot) =>
        new(
            snapshot.TradingDay,
            snapshot.Orders.Length,
            snapshot.Positions.Length,
            snapshot.Accounts.Length,
            snapshot.BestFxPrices.Count,
            snapshot.BestTradingPrices.Count);
}
