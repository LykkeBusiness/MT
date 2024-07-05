// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;

using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Common.Services;
using MarginTrading.OrderbookAggregator.Contracts.Messages;

namespace MarginTrading.Backend.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class ExternalExchangeOrderbookMessageHandler : IMessageHandler<ExternalExchangeOrderbookMessage>
    {
        private readonly IConvertService _convertService;
        private readonly IExternalOrderbookService _externalOrderbookService;

        public ExternalExchangeOrderbookMessageHandler(
            IConvertService convertService,
            IExternalOrderbookService externalOrderbookService)
        {
            _convertService = convertService;
            _externalOrderbookService = externalOrderbookService;
        }

        public Task Handle(ExternalExchangeOrderbookMessage message)
        {
            var orderbook = _convertService.Convert<ExternalExchangeOrderbookMessage, ExternalOrderBook>(message);
            _externalOrderbookService.SetOrderbook(orderbook);
            return Task.CompletedTask;
        }
    }
}