// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Common.Log;

using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Snapshots;
using MarginTrading.Common.Services;

using MarginTradingTests.Helpers;

using Moq;

using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class FinalSnapshotCalculatorTests
    {
        class FakeDraftSnapshotKeeper(DateTime tradingDay, DateTime timestamp) : IDraftSnapshotKeeper
        {
            public DateTime? TradingDay { get; set; } = tradingDay;

            public DateTime Timestamp { get; set; } = timestamp;

            public List<BestPriceContract> FxPrices => [];

            public List<BestPriceContract> CfdQuotes => [];

            public ValueTask<bool> ExistsAsync()
            {
                throw new NotImplementedException();
            }

            public ValueTask<List<MarginTradingAccount>> GetAccountsAsync() =>
                ValueTask.FromResult(new List<MarginTradingAccount>() { GetDumbMarginTradingAccount() });

            public ImmutableArray<Order> GetAllOrders() => [DumbDataGenerator.GenerateOrder()];

            public ImmutableArray<Order> GetPending()
            {
                throw new NotImplementedException();
            }

            public ImmutableArray<Position> GetPositions(string instrument)
            {
                throw new NotImplementedException();
            }

            public ImmutableArray<Position> GetPositions() => [DumbDataGenerator.GeneratePosition()];

            public ImmutableArray<Position> GetPositionsByFxAssetPairId(string fxAssetPairId)
            {
                throw new NotImplementedException();
            }

            public IDraftSnapshotKeeper Init(DateTime tradingDay)
            {
                throw new NotImplementedException();
            }

            public bool TryGetOrderById(string orderId, out Order order)
            {
                throw new NotImplementedException();
            }

            public Task UpdateAsync(ImmutableArray<Position> positions, ImmutableArray<Order> orders, ImmutableArray<MarginTradingAccount> accounts, IEnumerable<BestPriceContract> fxRates, IEnumerable<BestPriceContract> cfdQuotes) => Task.CompletedTask;
        }

        private Mock<ICfdCalculatorService> _cfdCalculatorMock;
        private Mock<ILog> _logMock;
        private Mock<IDateService> _dateServiceMock;
        private IDraftSnapshotKeeper _draftSnapshotKeeper;
        private Mock<IAccountsCacheService> _accountCacheServiceMock;

        [SetUp]
        public void SetUp()
        {
            _cfdCalculatorMock = new Mock<ICfdCalculatorService>();
            _logMock = new Mock<ILog>();
            _dateServiceMock = new Mock<IDateService>();
            _draftSnapshotKeeper = new FakeDraftSnapshotKeeper(DateTime.UtcNow, DateTime.UtcNow);
            _accountCacheServiceMock = new Mock<IAccountsCacheService>();

            _accountCacheServiceMock
                .Setup(c => c.GetAllWhereLiquidationIsRunning())
                .Returns(AsyncEnumerable.Empty<MarginTradingAccount>());
        }

        [Test]
        public async Task Run_Returns_Final_Snapshot_With_Same_Timestamp_As_Draft()
        {
            var sut = GetSut();

            var final = await sut.RunAsync(
                [GetDumbFxRate()],
                [GetDumbCfdQuote()],
                string.Empty);

            Assert.AreEqual(_draftSnapshotKeeper.Timestamp, final.Timestamp);
        }

        private IFinalSnapshotCalculator GetSut() => new FinalSnapshotCalculator(
            _cfdCalculatorMock.Object,
            _logMock.Object,
            _dateServiceMock.Object,
            _draftSnapshotKeeper,
            _accountCacheServiceMock.Object);

        private static ClosingFxRate GetDumbFxRate() =>
            new() { AssetId = "dumbAssetId", ClosePrice = 1 };

        private static ClosingAssetPrice GetDumbCfdQuote() =>
            new() { AssetId = "dumbAssetId", ClosePrice = 1 };

        private static MarginTradingAccount GetDumbMarginTradingAccount()
        {
            var result = new MarginTradingAccount();

            result.AccountFpl.ActualHash = 1;
            result.AccountFpl.CalculatedHash = 1;

            return result;
        }
    }
}