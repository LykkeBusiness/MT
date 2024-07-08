// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services.MatchingEngines;

namespace MarginTradingTests
{
    internal sealed class FakeMatchingEngineRoutesManager : IMatchingEngineRoutesManager
    {
        public bool UpdateRoutesCacheCalled { get; private set; } = false;

        public Task UpdateRoutesCacheAsync()
        {
            UpdateRoutesCacheCalled = true;
            return Task.CompletedTask;
        }

        public IMatchingEngineRoute FindRoute(
            string clientId,
            string tradingConditionId,
            string instrumentId,
            OrderDirection orderType)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleRiskManagerCommand(MatchingEngineRouteRisksCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleRiskManagerBlockTradingCommand(MatchingEngineRouteRisksCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}