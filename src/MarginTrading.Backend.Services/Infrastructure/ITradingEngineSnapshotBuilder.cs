// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Infrastructure;

/// <summary>
/// Builds a <see cref="TradingEngineSnapshotRaw"/> by collecting orders, positions, accounts and quotes.
/// </summary>
public interface ITradingEngineSnapshotBuilder
{
    ITradingEngineSnapshotBuilder WithOrders(
        ImmutableArray<Order> orders,
        ImmutableDictionary<string, ImmutableArray<Order>> relatedOrders);
    ITradingEngineSnapshotBuilder WithPositions(
        ImmutableArray<Position> positions,
        ImmutableDictionary<string, ImmutableArray<Order>> positionsRelatedOrders);
    ITradingEngineSnapshotBuilder WithAccounts(
        ImmutableArray<MarginTradingAccount> accounts,
        ImmutableArray<MarginTradingAccount> accountsInLiquidation);
    ITradingEngineSnapshotBuilder WithFxQuotes(ImmutableDictionary<string, InstrumentBidAskPair> quotes);
    ITradingEngineSnapshotBuilder WithQuotes(ImmutableDictionary<string, InstrumentBidAskPair> quotes);
    ITradingEngineSnapshotBuilder WithTradingDay(DateTime tradingDay);
    ITradingEngineSnapshotBuilder WithCorrelationId(string correlationId);
    ITradingEngineSnapshotBuilder WithTimestamp(DateTime timestamp);
    ITradingEngineSnapshotBuilder WithStatus(SnapshotStatus status);
    Task<TradingEngineSnapshotRaw> Build();
}
