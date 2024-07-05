// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Backend.Core.MarketMakerFeed;
using MarginTrading.Backend.Services;

namespace MarginTrading.Backend.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class NewOrdersHandler : IMessageHandler<MarketMakerOrderCommandsBatchMessage>
    {
        private readonly MarketMakerService _marketMakerService;

        public NewOrdersHandler(MarketMakerService marketMakerService)
        {
            _marketMakerService = marketMakerService;
        }

        public Task Handle(MarketMakerOrderCommandsBatchMessage message)
        {
            _marketMakerService.ProcessOrderCommands(message);
            return Task.CompletedTask;
        }
    }
}