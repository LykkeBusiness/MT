// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MarginTrading.AssetService.Contracts.Enums;
using MarginTrading.AssetService.Contracts.Messages;
using MarginTrading.Backend.MessageHandlers;

using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class SettingsChangedHandlerTests
    {
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
        public async Task Handle_NonTradingRouteSettingsType_DoesNotUpdateRoutesCache(SettingsTypeContract nonTradingRouteSettingsType)
        {
            var message = new SettingsChangedEvent { SettingsType = nonTradingRouteSettingsType};

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