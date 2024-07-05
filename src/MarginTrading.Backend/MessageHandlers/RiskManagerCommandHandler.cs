// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;

using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Services.MatchingEngines;

namespace MarginTrading.Backend.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class RiskManagerCommandHandler : IMessageHandler<MatchingEngineRouteRisksCommand>
    {
        private readonly IMatchingEngineRoutesManager _matchingEngineRoutesManager;

        public RiskManagerCommandHandler(IMatchingEngineRoutesManager matchingEngineRoutesManager)
        {
            _matchingEngineRoutesManager = matchingEngineRoutesManager;
        }

        public async Task Handle(MatchingEngineRouteRisksCommand message)
        {
            await _matchingEngineRoutesManager.HandleRiskManagerCommand(message);
        }
    }
}