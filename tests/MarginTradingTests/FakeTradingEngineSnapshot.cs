// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Common;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTradingTests
{
    internal class FakeTradingEngineSnapshot : TradingEngineSnapshot
    {
        public FakeTradingEngineSnapshot(
            DateTime tradingDay,
            string correlationId,
            DateTime timestamp,
            string ordersJson,
            string positionsJson,
            string accountsJson,
            string bestFxPricesJson,
            string bestTradingPricesJson,
            SnapshotStatus status) : base(
            tradingDay,
            correlationId,
            timestamp,
            ordersJson,
            positionsJson,
            accountsJson,
            bestFxPricesJson,
            bestTradingPricesJson,
            status)
        {

        }
    }
    
    /// <summary>
    /// This snapshot is empty, it has no orders, positions, accounts, fx rates or cfd quotes
    /// </summary>
    internal class EmptyTradingEngineSnapshot : FakeTradingEngineSnapshot
    {
        public EmptyTradingEngineSnapshot() : base(
            DateTime.UtcNow,
            string.Empty,
            DateTime.UtcNow,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            SnapshotStatus.Draft)
        {
        }
    }
    
    /// <summary>
    /// This snapshot has some orders, positions, accounts, fx rates and cfd quotes
    /// </summary>
    internal class DumbTradingEngineSnapshot : FakeTradingEngineSnapshot
    {
        public DumbTradingEngineSnapshot() : base(
            DateTime.UtcNow,
            string.Empty,
            DateTime.UtcNow,
            new[] { new { Id = "order-id-1" } }.ToJson(),
            new[] { new { Id = "position-id-1" } }.ToJson(),
            new List<MarginTradingAccount> { new MarginTradingAccount { Id = "1" } }.ToJson(),
            new[]
            {
                new
                {
                    Id = "EURUSD",
                    Timestamp = DateTime.UtcNow,
                    Bid = 8.7,
                    Ask = 8.8
                }
            }.ToDictionary(x => x.Id, x => x).ToJson(),
            new[]
            {
                new
                {
                    Id = "FACEBOOK",
                    Timestamp = DateTime.UtcNow,
                    Bid = 100,
                    Ask = 101
                }
            }.ToDictionary(x => x.Id, x => x).ToJson(),
            SnapshotStatus.Draft)
        {
        }
    }
}