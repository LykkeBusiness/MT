﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Linq;

using Common;
using Common.Log;

using Lykke.Common;

using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    public class StopOutConsumer : IEventConsumer<StopOutEventArgs>,
        IEventConsumer<LiquidationEndEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IOperationsLogService _operationsLogService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IDateService _dateService;
        private readonly MarginTradingSettings _settings;
        private readonly ILog _log;

        private readonly ConcurrentDictionary<string, DateTime> _lastNotifications = new();

        public StopOutConsumer(IThreadSwitcher threadSwitcher,
            IOperationsLogService operationsLogService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IDateService dateService,
            MarginTradingSettings settings,
            ILog log)
        {
            _threadSwitcher = threadSwitcher;
            _operationsLogService = operationsLogService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _dateService = dateService;

            _settings = settings;
            _log = log;
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<StopOutEventArgs>.ConsumeEvent(object sender, StopOutEventArgs ea)
        {
            var account = ea.Account;
            var eventTime = _dateService.Now();
            var accountMarginEventMessage = AccountMarginEventMessageConverter.Create(account, MarginEventTypeContract.Stopout, eventTime, ea.CorrelationId);

            _threadSwitcher.SwitchThread(async () =>
            {
                if (_lastNotifications.TryGetValue(account.Id, out var lastNotification)
                    && lastNotification.AddMinutes(_settings.Throttling.StopOutThrottlingPeriodMin) > eventTime)
                {
                    _log.WriteInfo(nameof(StopOutConsumer), nameof(IEventConsumer<StopOutEventArgs>.ConsumeEvent),
                        $"StopOut event is ignored for accountId {account.Id} because of throttling: event time {eventTime}, last notification was sent at {lastNotification}");
                    return;
                }

                _operationsLogService.AddLog("stopout", account.Id, "", ea.ToJson());

                await _rabbitMqNotifyService.AccountMarginEvent(accountMarginEventMessage);

                _lastNotifications.AddOrUpdate(account.Id, eventTime, (s, times) => eventTime);
            });
        }

        public void ConsumeEvent(object sender, LiquidationEndEventArgs ea)
        {
            if (ea.LiquidatedPositionIds.Any())
            {
                _lastNotifications.TryRemove(ea.AccountId, out _);
            }
        }
    }
}