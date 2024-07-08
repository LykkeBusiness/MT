using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Lykke.Snow.Common.WorkingDays;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.MessageHandlers;
using MarginTrading.Backend.Services.AssetPairs;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

namespace MarginTradingTests
{
    public class BrokerSettingsChangedHandlerTests
    {
        private class FakeScheduleSettingsCacheService : IScheduleSettingsCacheService
        {
            public bool AllSettingsUpdated { get; private set; }

            public Dictionary<string, List<CompiledScheduleTimeInterval>> GetCompiledAssetPairScheduleSettings()
            {
                throw new NotImplementedException();
            }

            public void CacheWarmUpIncludingValidation()
            {
                throw new NotImplementedException();
            }

            public void MarketsCacheWarmUp()
            {
                throw new NotImplementedException();
            }

            public Task UpdateAllSettingsAsync()
            {
                AllSettingsUpdated = true;
                return Task.CompletedTask;
            }

            public Task UpdateScheduleSettingsAsync()
            {
                throw new NotImplementedException();
            }

            public List<CompiledScheduleTimeInterval> GetPlatformTradingSchedule()
            {
                throw new NotImplementedException();
            }

            public Dictionary<string, List<CompiledScheduleTimeInterval>> GetMarketsTradingSchedule()
            {
                throw new NotImplementedException();
            }

            public Dictionary<string, MarketState> GetMarketState()
            {
                throw new NotImplementedException();
            }

            public void HandleMarketStateChanges(DateTime currentTime)
            {
                throw new NotImplementedException();
            }

            public bool TryGetPlatformCurrentDisabledInterval(out CompiledScheduleTimeInterval disabledInterval)
            {
                throw new NotImplementedException();
            }

            public InstrumentTradingStatus GetInstrumentTradingStatus(string assetPairId, TimeSpan scheduleCutOff)
            {
                throw new NotImplementedException();
            }

            public List<CompiledScheduleTimeInterval> GetMarketTradingScheduleByAssetPair(string assetPairId)
            {
                throw new NotImplementedException();
            }
        }

        private class FakeOvernightMarginService : IOvernightMarginService
        {
            public void ScheduleNext()
            {
            }
        }

        private class FakeScheduleControlService : IScheduleControlService
        {
            public void ScheduleNext()
            {
            }
        }

        private MarginTradingSettings _settings;
        private FakeScheduleSettingsCacheService _scheduleSettingsCache;
        private FakeOvernightMarginService _overnightMarginService;
        private FakeScheduleControlService _scheduleControlService;
        private ILogger<BrokerSettingsChangedHandler> _logger;
        private BrokerSettingsChangedHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _settings = new MarginTradingSettings { BrokerId = "TestBroker" };
            _scheduleSettingsCache = new FakeScheduleSettingsCacheService();
            _overnightMarginService = new FakeOvernightMarginService();
            _scheduleControlService = new FakeScheduleControlService();
            _logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<BrokerSettingsChangedHandler>();
            _handler = new BrokerSettingsChangedHandler(
                _settings,
                _scheduleSettingsCache,
                _overnightMarginService,
                _scheduleControlService,
                _logger);
        }

        [Test]
        public async Task Handle_EditionOnly_OldValueNull_UpdatesSettings()
        {
            var message = new BrokerSettingsChangedEvent
            {
                ChangeType = ChangeType.Edition,
                OldValue = null,
                NewValue = new BrokerSettingsContract { BrokerId = "TestBroker" }
            };

            await _handler.Handle(message);

            Assert.IsTrue(_scheduleSettingsCache.AllSettingsUpdated, "Settings were not updated");
        }

        [Test]
        public async Task Handle_EditionOnly_ScheduleDataChanged_UpdatesSettings()
        {
            var message = new BrokerSettingsChangedEvent
            {
                ChangeType = ChangeType.Edition,
                OldValue = new BrokerSettingsContract
                {
                    BrokerId = _settings.BrokerId,
                    Open = TimeSpan.FromHours(9),
                    Close = TimeSpan.FromHours(17),
                    Timezone = "UTC",
                    Holidays = new List<DateTime> { new DateTime(2024, 1, 1) },
                    Weekends = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
                    PlatformSchedule =
                        new PlatformScheduleContract
                        {
                            HalfWorkingDays = new List<WorkingDay> { new WorkingDay("2020-12-04 > 12:00:00") }
                        }
                },
                NewValue = new BrokerSettingsContract
                {
                    BrokerId = _settings.BrokerId,
                    Open = TimeSpan.FromHours(9),
                    Close = TimeSpan.FromHours(18), // Changed close time
                    Timezone = "UTC",
                    Holidays = new List<DateTime> { new DateTime(2024, 1, 1) },
                    Weekends = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
                    PlatformSchedule = new PlatformScheduleContract
                    {
                        HalfWorkingDays = new List<WorkingDay> { new WorkingDay("2020-12-04 > 12:00:00") }
                    }
                }
            };

            await _handler.Handle(message);

            Assert.IsTrue(_scheduleSettingsCache.AllSettingsUpdated, "UpdateAllSettingsAsync was not called");
        }

        [Test]
        public void Handle_UnexpectedChangeType_ThrowsArgumentOutOfRangeException()
        {
            var message = new BrokerSettingsChangedEvent
            {
                ChangeType = (ChangeType)999, // Invalid ChangeType
                OldValue = null,
                NewValue = new BrokerSettingsContract()
            };

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _handler.Handle(message));
        }
    }
}