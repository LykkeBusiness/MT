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

    public TradingEngineSnapshotSummary Summary => new(
        TradingDay,
        Orders.Length,
        Positions.Length,
        Accounts.Length,
        BestFxPrices.Count,
        BestTradingPrices.Count);
}
