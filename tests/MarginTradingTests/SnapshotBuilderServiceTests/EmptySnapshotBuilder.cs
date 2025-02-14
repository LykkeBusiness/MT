using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Infrastructure;

namespace MarginTradingTests.SnapshotBuilderServiceTests;

class EmptySnapshotBuilder : ITradingEngineSnapshotBuilder
{
    public Task<TradingEngineSnapshotRaw> Build()
    {
        return Task.FromResult(TradingEngineSnapshotRaw.Empty);
    }

    public ITradingEngineSnapshotBuilder CollectDataFrom(IOrderReaderBase orderReader)
    {
        return this;
    }

    public ITradingEngineSnapshotBuilder WithAccounts(ImmutableArray<MarginTradingAccount> accounts, ImmutableArray<MarginTradingAccount> accountsInLiquidation)
    {
        return this;
    }

    public ITradingEngineSnapshotBuilder WithCorrelationId(string correlationId)
    {
        return this;
    }

    public ITradingEngineSnapshotBuilder WithFxQuotes(ImmutableDictionary<string, InstrumentBidAskPair> quotes)
    {
        return this;
    }

    public ITradingEngineSnapshotBuilder WithOrders(ImmutableArray<Order> orders, ImmutableDictionary<string, ImmutableArray<Order>> relatedOrders)
    {
        return this;
    }

    public ITradingEngineSnapshotBuilder WithPositions(ImmutableArray<Position> positions, ImmutableDictionary<string, ImmutableArray<Order>> positionsRelatedOrders)
    {
        return this;
    }

    public ITradingEngineSnapshotBuilder WithQuotes(ImmutableDictionary<string, InstrumentBidAskPair> quotes)
    {
        return this;
    }

    public ITradingEngineSnapshotBuilder WithStatus(SnapshotStatus status)
    {
        return this;
    }

    public ITradingEngineSnapshotBuilder WithTimestamp(DateTime timestamp)
    {
        return this;
    }

    public ITradingEngineSnapshotBuilder WithTradingDay(DateTime tradingDay)
    {
        return this;
    }
}
