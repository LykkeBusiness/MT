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
/// Implementation of a trading engine snapshot builder.
/// </summary>
class TradingEngineSnapshotBuilder : ITradingEngineSnapshotBuilder
{
    private TradingEngineSnapshotRaw _snapshotRaw = TradingEngineSnapshotRaw.Empty;

    public ITradingEngineSnapshotBuilder WithTradingDay(DateTime tradingDay)
    {
        if (tradingDay == default)
            throw new InvalidOperationException("Trading day is not set");
        _snapshotRaw = _snapshotRaw with { TradingDay = tradingDay };
        return this;
    }

    public ITradingEngineSnapshotBuilder WithCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new InvalidOperationException("Correlation ID is not set");
        _snapshotRaw = _snapshotRaw with { CorrelationId = correlationId };
        return this;
    }

    public ITradingEngineSnapshotBuilder WithTimestamp(DateTime timestamp)
    {
        if (timestamp == default)
            throw new InvalidOperationException("Timestamp is not set");
        _snapshotRaw = _snapshotRaw with { Timestamp = timestamp };
        return this;
    }

    public ITradingEngineSnapshotBuilder WithStatus(SnapshotStatus status)
    {
        _snapshotRaw = _snapshotRaw with { Status = status };
        return this;
    }

    public ITradingEngineSnapshotBuilder WithOrders(
        ImmutableArray<Order> orders,
        ImmutableDictionary<string, ImmutableArray<Order>> relatedOrders)
    {
        if (orders.IsDefaultOrEmpty)
            throw new InvalidOperationException("Orders collection cannot be null or empty.");
        _snapshotRaw = _snapshotRaw with { Orders = orders, RelatedOrders = relatedOrders };
        return this;
    }

    public ITradingEngineSnapshotBuilder WithPositions(
        ImmutableArray<Position> positions,
        ImmutableDictionary<string, ImmutableArray<Order>> positionsRelatedOrders)
    {
        if (positions.IsDefaultOrEmpty)
            throw new InvalidOperationException("Positions collection cannot be null or empty.");
        _snapshotRaw = _snapshotRaw with { Positions = positions, PositionsRelatedOrders = positionsRelatedOrders };
        return this;
    }

    public ITradingEngineSnapshotBuilder WithAccounts(
        ImmutableArray<MarginTradingAccount> accounts,
        ImmutableArray<MarginTradingAccount> accountsInLiquidation)
    {
        if (accounts.IsDefaultOrEmpty)
            throw new InvalidOperationException("Accounts collection cannot be null or empty.");
        _snapshotRaw = _snapshotRaw with { Accounts = accounts, AccountsInLiquidation = accountsInLiquidation };
        return this;
    }

    public ITradingEngineSnapshotBuilder WithFxQuotes(ImmutableDictionary<string, InstrumentBidAskPair> quotes)
    {
        if (quotes == null || quotes.IsEmpty)
            throw new InvalidOperationException("Fx quotes collection cannot be null or empty.");
        _snapshotRaw = _snapshotRaw with { BestFxPrices = quotes };
        return this;
    }

    public ITradingEngineSnapshotBuilder WithQuotes(ImmutableDictionary<string, InstrumentBidAskPair> quotes)
    {
        if (quotes == null || quotes.IsEmpty)
            throw new InvalidOperationException("Quotes collection cannot be null or empty.");
        _snapshotRaw = _snapshotRaw with { BestTradingPrices = quotes };
        return this;
    }

    public Task<TradingEngineSnapshotRaw> Build()
    {
        if (_snapshotRaw == TradingEngineSnapshotRaw.Empty)
            throw new InvalidOperationException("Snapshot is not initialized.");
        (var result, _snapshotRaw) = (_snapshotRaw, TradingEngineSnapshotRaw.Empty);
        return Task.FromResult(result);
    }
}
