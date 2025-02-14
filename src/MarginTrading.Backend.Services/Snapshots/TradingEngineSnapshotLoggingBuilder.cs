// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Infrastructure;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Snapshots;

class TradingEngineSnapshotLoggingBuilder(
    ITradingEngineSnapshotBuilder decoratee,
    bool logBlockedMarginCalculation,
    ILogger<TradingEngineSnapshotLoggingBuilder> logger) : ITradingEngineSnapshotBuilder
{
    private readonly ITradingEngineSnapshotBuilder _decoratee =
        decoratee ?? throw new ArgumentNullException(nameof(decoratee));
    private readonly ILogger<TradingEngineSnapshotLoggingBuilder> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public ITradingEngineSnapshotBuilder CollectDataFrom(IOrderReaderBase orderReader) =>
        _decoratee.CollectDataFrom(orderReader);


    public ITradingEngineSnapshotBuilder WithAccounts(
        ImmutableArray<MarginTradingAccount> accounts,
        ImmutableArray<MarginTradingAccount> accountsInLiquidation)
    {
        var result = _decoratee.WithAccounts(accounts, accountsInLiquidation);
        _logger.LogInformation("Preparing data... {AccountsCount} accounts prepared.", accounts.Length);
        return result;
    }

    public ITradingEngineSnapshotBuilder WithCorrelationId(string correlationId) =>
        _decoratee.WithCorrelationId(correlationId);

    public ITradingEngineSnapshotBuilder WithFxQuotes(ImmutableDictionary<string, InstrumentBidAskPair> quotes)
    {
        var result = _decoratee.WithFxQuotes(quotes);
        _logger.LogInformation("Preparing data... {QuotesCount} best FX prices prepared.", quotes.Count);
        return result;
    }

    public ITradingEngineSnapshotBuilder WithOrders(
        ImmutableArray<Order> orders,
        ImmutableDictionary<string, ImmutableArray<Order>> relatedOrders)
    {
        var result = _decoratee.WithOrders(orders, relatedOrders);
        _logger.LogInformation("Preparing data... {OrdersCount} orders prepared.", orders.Length);
        return result;
    }

    public ITradingEngineSnapshotBuilder WithPositions(
        ImmutableArray<Position> positions,
        ImmutableDictionary<string, ImmutableArray<Order>> positionsRelatedOrders)
    {
        var result = _decoratee.WithPositions(positions, positionsRelatedOrders);
        _logger.LogInformation("Preparing data... {PositionsCount} positions prepared.", positions.Length);
        return result;
    }

    public ITradingEngineSnapshotBuilder WithQuotes(ImmutableDictionary<string, InstrumentBidAskPair> quotes)
    {
        var result = _decoratee.WithQuotes(quotes);
        _logger.LogInformation("Preparing data... {QuotesCount} best trading prices prepared.", quotes.Count);
        return result;
    }

    public ITradingEngineSnapshotBuilder WithStatus(SnapshotStatus status)
    {
        var result = _decoratee.WithStatus(status);
        _logger.LogInformation("Preparing data... Snapshot status: {Status}", status);
        return result;
    }

    public ITradingEngineSnapshotBuilder WithTimestamp(DateTime timestamp)
    {
        var result = _decoratee.WithTimestamp(timestamp);
        _logger.LogInformation("Preparing data... Snapshot timestamp: {Timestamp}", timestamp);
        return result;
    }

    public ITradingEngineSnapshotBuilder WithTradingDay(DateTime tradingDay)
    {
        var result = _decoratee.WithTradingDay(tradingDay);
        _logger.LogInformation("Preparing data... Snapshot trading day: {TradingDay}", tradingDay);
        return result;
    }

    public async Task<TradingEngineSnapshotRaw> Build()
    {
        var snapshot = await _decoratee.Build();

        if (logBlockedMarginCalculation)
            LogBlockedMargin(snapshot);

        return snapshot;
    }

    private void LogBlockedMargin(TradingEngineSnapshotRaw snapshot)
    {
        var positionsByAccount = snapshot.Positions
            .GroupBy(p => p.AccountId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var account in snapshot.Accounts)
        {
            var margin = account.GetUsedMargin();
            if (margin == 0)
                continue;

            _logger.LogInformation(
                "Account {AccountId}, TotalBlockedMargin {Margin}, {Info}",
                account.Id,
                margin,
                account.LogInfo);

            if (positionsByAccount.TryGetValue(account.Id, out var accountPositions))
            {
                foreach (var p in accountPositions)
                {
                    _logger.LogInformation(
                        "Account {AccountId}, Position {PositionId}, {Info}",
                        account.Id,
                        p.Id,
                        p.FplData.LogInfo);
                }
            }
        }
    }
}
