// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Quotes;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.MessageHandlers;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.OrderbookAggregator.Contracts.Messages;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

namespace MarginTradingTests
{
    public class FxRateExternalExchangeOrderbookHandlerTests
    {
        private class FakeAssetPairDayOffService : IAssetPairDayOffService
        {
            public InstrumentTradingStatus IsAssetTradingDisabledResult { get; set; }

            public InstrumentTradingStatus IsAssetTradingDisabled(string assetPairId)
            {
                return IsAssetTradingDisabledResult;
            }

            public bool ArePendingOrdersDisabled(string assetPairId)
            {
                throw new NotImplementedException();
            }
        }

        private class FakeFxRateCacheService : IFxRateCacheService
        {
            public InstrumentBidAskPair LastQuote { get; private set; }

            public InstrumentBidAskPair GetQuote(string instrument)
            {
                throw new NotImplementedException();
            }

            public Dictionary<string, InstrumentBidAskPair> GetAllQuotes()
            {
                throw new NotImplementedException();
            }

            public void SetQuote(InstrumentBidAskPair pair)
            {
                LastQuote = pair;
            }

            public RemoveQuoteError RemoveQuote(string assetPairId)
            {
                throw new NotImplementedException();
            }
        }

        private class FakeEventChannel : IEventChannel<FxBestPriceChangeEventArgs>
        {
            public FxBestPriceChangeEventArgs LastEvent { get; private set; }

            public void SendEvent(object sender, FxBestPriceChangeEventArgs e)
            {
                LastEvent = e;
            }
        }

        private MarginTradingSettings _settings;
        private FakeAssetPairDayOffService _assetPairDayOffService;
        private FakeFxRateCacheService _fxRateCacheService;
        private FakeEventChannel _fxBestPriceChangeEventChannel;
        private ILogger<FxRateExternalExchangeOrderbookHandler> _logger;
        private FxRateExternalExchangeOrderbookHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _settings = new MarginTradingSettings
            {
                OrderbookValidation = new OrderbookValidationSettings
                {
                    ValidateInstrumentStatusForEodFx = true, 
                    ValidateInstrumentStatusForTradingFx = true
                }
            };
            _assetPairDayOffService = new FakeAssetPairDayOffService();
            _fxRateCacheService = new FakeFxRateCacheService();
            _fxBestPriceChangeEventChannel = new FakeEventChannel();
            _logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<FxRateExternalExchangeOrderbookHandler>();
            _handler = new FxRateExternalExchangeOrderbookHandler(
                _settings,
                _assetPairDayOffService,
                _logger,
                _fxBestPriceChangeEventChannel,
                _fxRateCacheService);
        }

        [Test]
        public async Task Handle_NotEodOrderbook_AssetNotTradable_SkipsMessage()
        {
            var message = new FxRateExternalExchangeOrderbookMessage
            {
                ExchangeName = "NotEodExchange",
                AssetPairId = "TestAsset",
                Timestamp = DateTime.UtcNow,
                Bids = new List<VolumePrice> { new VolumePrice { Price = 1.1m, Volume = 100 } },
                Asks = new List<VolumePrice> { new VolumePrice { Price = 1.2m, Volume = 100 } }
            };
            _assetPairDayOffService.IsAssetTradingDisabledResult =
                InstrumentTradingStatus.Disabled(InstrumentTradingDisabledReason.None);

            await _handler.Handle(message);
            
            Assert.IsNull(_fxBestPriceChangeEventChannel.LastEvent, "Event should not be sent");
        }
        
        [Test]
        public async Task Handle_EodOrderbook_AssetTradable_SkipsMessage()
        {
            var message = new FxRateExternalExchangeOrderbookMessage
            {
                ExchangeName = ExternalOrderbookService.EodExternalExchange,
                AssetPairId = "TestAsset",
                Timestamp = DateTime.UtcNow,
                Bids = new List<VolumePrice> { new VolumePrice { Price = 1.1m, Volume = 100 } },
                Asks = new List<VolumePrice> { new VolumePrice { Price = 1.2m, Volume = 100 } }
            };
            _assetPairDayOffService.IsAssetTradingDisabledResult = InstrumentTradingStatus.Enabled();

            await _handler.Handle(message);
            
            Assert.IsNull(_fxBestPriceChangeEventChannel.LastEvent, "Event should not be sent");
        }
        
        [Test]
        public async Task Handle_EodOrderBook_AssetNotTradable_ProcessesMessage()
        {
            var message = new FxRateExternalExchangeOrderbookMessage
            {
                ExchangeName = ExternalOrderbookService.EodExternalExchange,
                AssetPairId = "TestAsset",
                Timestamp = DateTime.UtcNow,
                Bids = new List<VolumePrice> { new VolumePrice { Price = 1.1m, Volume = 100 } },
                Asks = new List<VolumePrice> { new VolumePrice { Price = 1.2m, Volume = 100 } }
            };
            _assetPairDayOffService.IsAssetTradingDisabledResult =
                InstrumentTradingStatus.Disabled(InstrumentTradingDisabledReason.None);

            await _handler.Handle(message);
            
            Assert.IsNotNull(_fxBestPriceChangeEventChannel.LastEvent, "Event should be sent");
        }
    }
}