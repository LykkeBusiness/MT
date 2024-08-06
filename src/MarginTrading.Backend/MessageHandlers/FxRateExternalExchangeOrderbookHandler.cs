// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Common;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Common.Extensions;
using MarginTrading.OrderbookAggregator.Contracts.Messages;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class FxRateExternalExchangeOrderbookHandler : IMessageHandler<FxRateExternalExchangeOrderbookMessage>
    {
        private readonly MarginTradingSettings _settings;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly IEventChannel<FxBestPriceChangeEventArgs> _fxBestPriceChangeEventChannel;
        private readonly IFxRateCacheService _fxRateCacheService;
        private readonly ILogger<FxRateExternalExchangeOrderbookHandler> _logger;

        public FxRateExternalExchangeOrderbookHandler(
            MarginTradingSettings settings,
            IAssetPairDayOffService assetPairDayOffService,
            ILogger<FxRateExternalExchangeOrderbookHandler> logger,
            IEventChannel<FxBestPriceChangeEventArgs> fxBestPriceChangeEventChannel,
            IFxRateCacheService fxRateCacheService)
        {
            _settings = settings;
            _assetPairDayOffService = assetPairDayOffService;
            _fxBestPriceChangeEventChannel = fxBestPriceChangeEventChannel;
            _fxRateCacheService = fxRateCacheService;
            _logger = logger;
        }

        public Task Handle(FxRateExternalExchangeOrderbookMessage message)
        {
            var isEodOrderbook = message.ExchangeName == ExternalOrderbookService.EodExternalExchange;

            if (_settings.OrderbookValidation.ValidateInstrumentStatusForEodFx && isEodOrderbook ||
                _settings.OrderbookValidation.ValidateInstrumentStatusForTradingFx && !isEodOrderbook)
            {
                var isAssetTradingDisabled = _assetPairDayOffService.IsAssetTradingDisabled(message.AssetPairId);

                // we should process normal orderbook only if asset is currently tradable
                if (_settings.OrderbookValidation.ValidateInstrumentStatusForTradingFx && isAssetTradingDisabled &&
                    !isEodOrderbook)
                {
                    return Task.CompletedTask;
                }

                // and process EOD orderbook only if asset is currently not tradable
                if (_settings.OrderbookValidation.ValidateInstrumentStatusForEodFx && !isAssetTradingDisabled &&
                    isEodOrderbook)
                {
                    _logger.LogWarning(
                        "EOD FX quote for {AssetPairId} is skipped, because instrument is within trading hours",
                        message.AssetPairId);

                    return Task.CompletedTask;
                }
            }

            var bidAskPair = CreatePair(message);

            if (bidAskPair == null)
            {
                return Task.CompletedTask;
            }

            _fxRateCacheService.SetQuote(bidAskPair);

            _fxBestPriceChangeEventChannel.SendEvent(this, new FxBestPriceChangeEventArgs(bidAskPair));

            return Task.CompletedTask;
        }

        private InstrumentBidAskPair CreatePair(FxRateExternalExchangeOrderbookMessage message)
        {
            if (!ValidateOrderbook(message))
            {
                return null;
            }

            var ask = GetBestPrice(true, message.Asks);
            var bid = GetBestPrice(false, message.Bids);

            return ask == null || bid == null
                ? null
                : new InstrumentBidAskPair
                {
                    Instrument = message.AssetPairId,
                    Date = message.Timestamp,
                    Ask = ask.Value,
                    Bid = bid.Value
                };
        }

        private bool ValidateOrderbook(FxRateExternalExchangeOrderbookMessage orderbook)
        {
            try
            {
                orderbook.AssetPairId.RequiredNotNullOrWhiteSpace("orderbook.AssetPairId");
                orderbook.ExchangeName.RequiredNotNullOrWhiteSpace("orderbook.ExchangeName");
                orderbook.RequiredNotNull(nameof(orderbook));

                orderbook.Bids.RequiredNotNullOrEmpty("orderbook.Bids");
                orderbook.Bids.RemoveAll(e => e == null || e.Price <= 0 || e.Volume == 0);
                orderbook.Bids.RequiredNotNullOrEmptyEnumerable("orderbook.Bids");

                orderbook.Asks.RequiredNotNullOrEmpty("orderbook.Asks");
                orderbook.Asks.RemoveAll(e => e == null || e.Price <= 0 || e.Volume == 0);
                orderbook.Asks.RequiredNotNullOrEmptyEnumerable("orderbook.Asks");

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Orderbook validation error, orderbook: {OrderBookJson}", orderbook.ToJson());
                return false;
            }
        }
        
        private static decimal? GetBestPrice(bool isBuy, IReadOnlyCollection<VolumePrice> prices)
        {
            if (!prices.Any())
                return null;
            return isBuy
                ? prices.Min(x => x.Price)
                : prices.Max(x => x.Price);
        }
    }
}