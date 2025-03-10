// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Snapshots;

using MarginTradingTests.Helpers;

using Moq;

using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class DraftSnapshotKeeperTests
    {
        private Mock<ITradingEngineSnapshotsRepository> _repositoryWithEmptySnapshot;
        private Mock<ITradingEngineSnapshotsRepository> _repositoryWithDumbSnapshot;

        private static readonly object[] UpdateWithInvalidArgumentsCases =
        {
            new object[] { null, null, null, null, null },
            new object[]
            {
                ImmutableArray.Create(new Position()),
                ImmutableArray.Create(DumbDataGenerator.GenerateOrder()),
                null,
                new List<BestPriceContract>(),
                new List<BestPriceContract>(),
            },
            new object[]
            {
                ImmutableArray.Create(new Position()),
                ImmutableArray.Create(DumbDataGenerator.GenerateOrder()),
                ImmutableArray.Create<MarginTradingAccount>(),
                new List<BestPriceContract>(),
                new List<BestPriceContract>(),
            },
        };

        [SetUp]
        public void SetUp()
        {
            _repositoryWithEmptySnapshot = new Mock<ITradingEngineSnapshotsRepository>();
            _repositoryWithEmptySnapshot
                .Setup(r => r.DraftExistsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(true);
            _repositoryWithEmptySnapshot
                .Setup(r => r.GetLastDraftAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new EmptyTradingEngineSnapshot());

            _repositoryWithDumbSnapshot = new Mock<ITradingEngineSnapshotsRepository>();
            _repositoryWithDumbSnapshot
                .Setup(r => r.DraftExistsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(true);
            _repositoryWithDumbSnapshot
                .Setup(r => r.GetLastDraftAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new DumbTradingEngineSnapshot());
        }

        [Test]
        public void AccessTradingDay_BeforeInitialization_ReturnsNull()
        {
            var keeper = new DraftSnapshotKeeper(_repositoryWithEmptySnapshot.Object);

            Assert.That(keeper.TradingDay, Is.Null);
        }

        [Test]
        public void AccessTimestamp_BeforeInitialization_ThrowsException()
        {
            var keeper = new DraftSnapshotKeeper(_repositoryWithEmptySnapshot.Object);

            Assert.Throws<InvalidOperationException>(() =>
            {
                var _ = keeper.Timestamp;
            });
        }

        [Test]
        public void Exists_BeforeInitialization_ThrowsException()
        {
            var keeper = new DraftSnapshotKeeper(_repositoryWithEmptySnapshot.Object);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await keeper.ExistsAsync());
        }

        [Test]
        public async Task Exists_ChecksIfDraftExists()
        {
            var keeper = GetSutWithEmptySnapshot();

            var _ = await keeper.ExistsAsync();

            _repositoryWithEmptySnapshot.Verify(r => r.DraftExistsAsync(It.IsAny<DateTime>()), Times.Once);
        }

        [Test]
        public void GetAccounts_BeforeInitialization_ThrowsException()
        {
            var keeper = new DraftSnapshotKeeper(_repositoryWithEmptySnapshot.Object);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await keeper.GetAccountsAsync());
        }

        [Test]
        public void GetAccounts_NoDraft_ThrowsException()
        {
            _repositoryWithEmptySnapshot
                .Setup(r => r.GetLastDraftAsync(It.IsAny<DateTime>()))
                .ReturnsAsync((TradingEngineSnapshot)null);

            var keeper = GetSutWithEmptySnapshot();

            Assert.ThrowsAsync<TradingSnapshotDraftNotFoundException>(async () => await keeper.GetAccountsAsync());
        }

        [Test]
        public async Task GetAccounts_ReturnsCached_WhenAccessed_MoreThanOnce()
        {
            var keeper = GetSutWithDumbSnapshot();

            var accounts = await keeper.GetAccountsAsync();

            Assert.NotNull(accounts);
            Assert.That(accounts.Exists(a => a.Id == "1"));

            _repositoryWithEmptySnapshot
                .Setup(r => r.GetLastDraftAsync(It.IsAny<DateTime>()))
                .ReturnsAsync((TradingEngineSnapshot)null);

            var accountsCached = await keeper.GetAccountsAsync();
            Assert.That(accounts.SequenceEqual(accountsCached));
        }

        [TestCaseSource(nameof(UpdateWithInvalidArgumentsCases))]
        public void Update_InvalidArguments_ThrowsException(
            ImmutableArray<Position> positions,
            ImmutableArray<Order> orders,
            ImmutableArray<MarginTradingAccount> accounts,
            IList<BestPriceContract> fxRates,
            IList<BestPriceContract> cfdQuotes)
        {
            var keeper = GetSutWithEmptySnapshot();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await keeper.UpdateAsync(positions, orders, accounts, fxRates, cfdQuotes));
        }

        [Test]
        public void When_Positions_NotEmpty_Update_WithEmptyList_ThrowsException()
        {
            var keeper = GetSutWithDumbSnapshot();

            var actualPositions = keeper.GetPositions();
            CollectionAssert.IsNotEmpty(actualPositions);

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await keeper.UpdateAsync(ImmutableArray<Position>.Empty,
                    ImmutableArray.Create(DumbDataGenerator.GenerateOrder()),
                    ImmutableArray.Create(new MarginTradingAccount()),
                    new List<BestPriceContract>(),
                    new List<BestPriceContract>()));
        }

        [Test]
        public void When_Orders_NotEmpty_Update_WithEmptyList_ThrowsException()
        {
            var keeper = GetSutWithDumbSnapshot();

            var actualOrders = keeper.GetAllOrders();
            CollectionAssert.IsNotEmpty(actualOrders);

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await keeper.UpdateAsync(ImmutableArray.Create(new Position()),
                    ImmutableArray<Order>.Empty,
                    ImmutableArray.Create(new MarginTradingAccount()),
                    new List<BestPriceContract>(),
                    new List<BestPriceContract>()));
        }

        private IDraftSnapshotKeeper GetSutWithEmptySnapshot() => new DraftSnapshotKeeper(_repositoryWithEmptySnapshot.Object).Init(DateTime.UtcNow);
        private IDraftSnapshotKeeper GetSutWithDumbSnapshot() => new DraftSnapshotKeeper(_repositoryWithDumbSnapshot.Object).Init(DateTime.UtcNow);
    }
}