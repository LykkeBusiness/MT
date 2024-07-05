// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;

using MarginTrading.AssetService.Contracts.Enums;
using MarginTrading.AssetService.Contracts.Messages;
using MarginTrading.Backend.Services.MatchingEngines;

namespace MarginTrading.Backend.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class SettingsChangedHandler : IMessageHandler<SettingsChangedEvent>
    {
        private readonly IMatchingEngineRoutesManager _matchingEngineRoutesManager;

        public SettingsChangedHandler(IMatchingEngineRoutesManager matchingEngineRoutesManager)
        {
            _matchingEngineRoutesManager = matchingEngineRoutesManager;
        }

        public async Task Handle(SettingsChangedEvent message)
        {
            if (message.SettingsType != SettingsTypeContract.TradingRoute)
                return;
            
            await _matchingEngineRoutesManager.UpdateRoutesCacheAsync();
        }
    }
}