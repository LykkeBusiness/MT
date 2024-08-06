// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.MessageHandlers
{
    public class BrokerSettingsChangedHandler : IMessageHandler<BrokerSettingsChangedEvent>
    {
        private readonly MarginTradingSettings _settings;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCache;
        private readonly IOvernightMarginService _overnightMarginService;
        private readonly IScheduleControlService _scheduleControlService;
        private readonly ILogger<BrokerSettingsChangedHandler> _logger;

        public BrokerSettingsChangedHandler(
            MarginTradingSettings settings,
            IScheduleSettingsCacheService scheduleSettingsCache,
            IOvernightMarginService overnightMarginService,
            IScheduleControlService scheduleControlService,
            ILogger<BrokerSettingsChangedHandler> logger)
        {
            _settings = settings;
            _scheduleSettingsCache = scheduleSettingsCache;
            _overnightMarginService = overnightMarginService;
            _scheduleControlService = scheduleControlService;
            _logger = logger;
        }

        [UsedImplicitly]
        public async Task Handle(BrokerSettingsChangedEvent message)
        {
            _logger.LogInformation("BrokerSettingsChangedEvent received");
            
            switch (message.ChangeType)
            {
                case ChangeType.Creation:
                case ChangeType.Deletion:
                    break;
                case ChangeType.Edition:
                    if (message.OldValue == null || IsScheduleDataChanged(message.OldValue, message.NewValue, _settings.BrokerId))
                    {
                        _logger.LogInformation("BrokerSettingsChangedEvent: schedule data changed");
                        await _scheduleSettingsCache.UpdateAllSettingsAsync();
                        _overnightMarginService.ScheduleNext();
                        _scheduleControlService.ScheduleNext();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(message.ChangeType), 
                        $@"Unexpected ChangeType: {message.ChangeType.ToString()}");
            }
        }

        private static bool IsScheduleDataChanged(BrokerSettingsContract oldSettings, BrokerSettingsContract newSettings, string brokerId)
        {
            var brokerConditionGuard = newSettings.BrokerId.Equals(brokerId, StringComparison.InvariantCultureIgnoreCase);
            if (!brokerConditionGuard) return false;

            var isSameSchedule = oldSettings.Open == newSettings.Open &&
                                 oldSettings.Close == newSettings.Close &&
                                 oldSettings.Timezone == newSettings.Timezone &&
                                 oldSettings.Holidays.SequenceEqual(newSettings.Holidays) &&
                                 oldSettings.Weekends.SequenceEqual(newSettings.Weekends) &&
                                 oldSettings.PlatformSchedule.HalfWorkingDays.SequenceEqual(newSettings.PlatformSchedule.HalfWorkingDays);

            return !isSameSchedule;
        }
    }
}