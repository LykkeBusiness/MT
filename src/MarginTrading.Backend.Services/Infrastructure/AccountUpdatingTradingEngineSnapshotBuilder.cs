// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;

using MoreLinq;

namespace MarginTrading.Backend.Services.Infrastructure;

class AccountUpdatingTradingEngineSnapshotBuilder(ITradingEngineSnapshotBuilder decoratee) : ITradingEngineSnapshotBuilder
{
    private readonly ITradingEngineSnapshotBuilder _decoratee =
        decoratee ?? throw new ArgumentNullException(nameof(decoratee));

    public Task<TradingEngineSnapshotRaw> Build() => _decoratee.Build();

    public ITradingEngineSnapshotBuilder WithAccounts(
        ImmutableArray<MarginTradingAccount> accounts,
        ImmutableArray<MarginTradingAccount> accountsInLiquidation)
    {
        var result = _decoratee.WithAccounts(accounts, accountsInLiquidation);
        // Forcing all account caches to be updated after trading is closed - after all events have been processed
        // To ensure all cache data is updated with most up-to date data for all accounts.
        accounts.ForEach(a => a.CacheNeedsToBeUpdated());
        return result;
    }

    public ITradingEngineSnapshotBuilder WithCorrelationId(string correlationId) =>
        _decoratee.WithCorrelationId(correlationId);

    public ITradingEngineSnapshotBuilder WithFxQuotes(ImmutableDictionary<string, InstrumentBidAskPair> quotes) =>
        _decoratee.WithFxQuotes(quotes);

    public ITradingEngineSnapshotBuilder WithOrders(
        ImmutableArray<Order> orders,
        ImmutableDictionary<string, ImmutableArray<Order>> relatedOrders) =>
        _decoratee.WithOrders(orders, relatedOrders);

    public ITradingEngineSnapshotBuilder WithPositions(
        ImmutableArray<Position> positions,
        ImmutableDictionary<string, ImmutableArray<Order>> positionsRelatedOrders) =>
        _decoratee.WithPositions(positions, positionsRelatedOrders);

    public ITradingEngineSnapshotBuilder WithQuotes(ImmutableDictionary<string, InstrumentBidAskPair> quotes) =>
        _decoratee.WithQuotes(quotes);

    public ITradingEngineSnapshotBuilder WithStatus(SnapshotStatus status) =>
        _decoratee.WithStatus(status);

    public ITradingEngineSnapshotBuilder WithTimestamp(DateTime timestamp) =>
        _decoratee.WithTimestamp(timestamp);

    public ITradingEngineSnapshotBuilder WithTradingDay(DateTime tradingDay) =>
        _decoratee.WithTradingDay(tradingDay);
}
