// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;

using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class StpAggregatorExternalExchangeOrderbookMessageHandler : IMessageHandler<StpAggregatorExternalExchangeOrderbookMessage>
    {
        private readonly IConvertService _convertService;
        private readonly IExternalOrderbookService _externalOrderbookService;

        public StpAggregatorExternalExchangeOrderbookMessageHandler(
            IConvertService convertService,
            IExternalOrderbookService externalOrderbookService)
        {
            _convertService = convertService;
            _externalOrderbookService = externalOrderbookService;
        }

        public Task Handle(StpAggregatorExternalExchangeOrderbookMessage message)
        {
            var orderbook = _convertService.Convert<StpAggregatorExternalExchangeOrderbookMessage, ExternalOrderBook>(message);
            _externalOrderbookService.SetOrderbook(orderbook);
            return Task.CompletedTask;
        }
    }
}