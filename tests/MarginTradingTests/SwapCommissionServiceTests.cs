﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.AssetService.Contracts;
using MarginTrading.AssetService.Contracts.TradingConditions;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class SwapCommissionServiceTests : BaseTests
    {
        private ICommissionService _swapService;
        private ITradingInstrumentsApi _tradingInstruments;
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private TradingInstrumentsManager _accountAssetsManager;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();

            _accountAssetsManager = Container.Resolve<TradingInstrumentsManager>();
            _swapService = Container.Resolve<ICommissionService>();
            _tradingInstruments = Container.Resolve<ITradingInstrumentsApi>();
            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
        }

        [Test]
        public async Task Is_Swaps_Correct()
        {
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "EURUSD", Bid = 1.02M, Ask = 1.04M }));

            var instrumentContract = new TradingInstrumentContract
            {
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                Instrument = "EURUSD",
                SwapLong = 100,
                SwapShort = 100,
                InitLeverage = 100,
                MaintenanceLeverage = 150,
                MarginRatePercent = 0.67M
            };

            Mock.Get(_tradingInstruments).Setup(s => s.List(It.IsAny<string>()))
                .ReturnsAsync(new List<TradingInstrumentContract> {instrumentContract});

            await _accountAssetsManager.UpdateTradingInstrumentsCacheAsync();

            var dayPosition = new Position(Guid.NewGuid().ToString("N"), 0, "EURUSD", 20, Accounts[0].Id,
                MarginTradingTestsUtils.TradingConditionId, Accounts[0].BaseAssetId, null, MatchingEngineConstants.DefaultMm,
                new DateTime(2017, 01, 01, 20, 50, 0), "OpenTrade", OrderType.Market, 20, 1, 1, "USD", 1,
                [], "LYKKETEST", OriginatorType.Investor, "", "EURUSD", FxToAssetPairDirection.Straight, "", false);

            dayPosition.SetCommissionRates(100, 0, 0, 1);

            dayPosition.StartClosing(new DateTime(2017, 01, 02, 20, 50, 0), PositionCloseReason.Close,
                OriginatorType.Investor, "");
            dayPosition.Close(new DateTime(2017, 01, 02, 20, 50, 0), MatchingEngineConstants.DefaultMm, 2, 1, 1, OriginatorType.Investor,
                PositionCloseReason.Close, "", "CloseTrade");

            var swapsForDay = _swapService.GetSwaps(dayPosition);

            var twoDayPosition = new Position(Guid.NewGuid().ToString("N"), 0, "EURUSD", 20, Accounts[0].Id,
                MarginTradingTestsUtils.TradingConditionId, Accounts[0].BaseAssetId, null, MatchingEngineConstants.DefaultMm,
                new DateTime(2017, 01, 01, 20, 50, 0), "OpenTrade", OrderType.Market, 20, 1, 1, "USD", 1,
                new ImmutableArray<RelatedOrderInfo>(), "LYKKETEST", OriginatorType.Investor, "", "EURUSD", FxToAssetPairDirection.Straight, "", false);

            twoDayPosition.SetCommissionRates(100, 0, 0, 1);

            twoDayPosition.StartClosing(new DateTime(2017, 01, 03, 20, 50, 0), PositionCloseReason.Close,
                OriginatorType.Investor, "");
            twoDayPosition.Close(new DateTime(2017, 01, 03, 20, 50, 0), MatchingEngineConstants.DefaultMm, 2, 1, 1, OriginatorType.Investor,
                PositionCloseReason.Close, "", "CloseTrade");

            var swapsFor2Days = _swapService.GetSwaps(twoDayPosition);

            Assert.AreEqual(5.70m, swapsForDay);
            Assert.AreEqual(11.40m, swapsFor2Days);
        }
    }
}
