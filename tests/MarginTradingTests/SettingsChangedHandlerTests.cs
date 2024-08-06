// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MarginTrading.AssetService.Contracts.Enums;
using MarginTrading.AssetService.Contracts.Messages;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.MessageHandlers;
using MarginTrading.Backend.Services.MatchingEngines;

using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class SettingsChangedHandlerTests
    {
        private class FakeMatchingEngineRoutesManager : IMatchingEngineRoutesManager
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
        
        private FakeMatchingEngineRoutesManager _fakeMatchingEngineRoutesManager;
        private SettingsChangedHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _fakeMatchingEngineRoutesManager = new FakeMatchingEngineRoutesManager();
            _handler = new SettingsChangedHandler(_fakeMatchingEngineRoutesManager);
        }

        [Test]
        public async Task Handle_TradingRouteSettingsType_UpdatesRoutesCache()
        {
            var message = new SettingsChangedEvent { SettingsType = SettingsTypeContract.TradingRoute };

            await _handler.Handle(message);

            Assert.IsTrue(_fakeMatchingEngineRoutesManager.UpdateRoutesCacheCalled);
        }

        [Test]
        [TestCaseSource(nameof(NonTradingRouteSettingsTypes))]
        public async Task Handle_NonTradingRouteSettingsType_DoesNotUpdateRoutesCache(
            SettingsTypeContract nonTradingRouteSettingsType)
        {
            var message = new SettingsChangedEvent { SettingsType = nonTradingRouteSettingsType };

            await _handler.Handle(message);

            Assert.IsFalse(_fakeMatchingEngineRoutesManager.UpdateRoutesCacheCalled);
        }

        private static IEnumerable<SettingsTypeContract> NonTradingRouteSettingsTypes()
        {
            return Enum.GetValues(typeof(SettingsTypeContract))
                .Cast<SettingsTypeContract>()
                .Where(e => e != SettingsTypeContract.TradingRoute);
        }
    }
}