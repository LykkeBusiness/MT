﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Quotes
{
    public class QuotesMonitor : TimerPeriod
    {
        private readonly ILog _log;
        private readonly MarginTradingSettings _marginSettings;
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IDateService _dateService;
        private readonly IAssetPairDayOffService _dayOffService;
        private const int NotificationRepeatTimeoutCoef = 5;
        
        private readonly Dictionary<string, OutdatedQuoteInfo> _outdatedQuotes;

        public QuotesMonitor(ILog log, 
            MarginTradingSettings marginSettings,
            IQuoteCacheService quoteCacheService,
            IDateService dateService,
            IAssetPairDayOffService dayOffService) 
            : base("QuotesMonitor", 60000, log)
        {
            _log = log;
            _marginSettings = marginSettings;
            _quoteCacheService = quoteCacheService;
            _dateService = dateService;
            _dayOffService = dayOffService;
            _outdatedQuotes = new Dictionary<string, OutdatedQuoteInfo>();
        }

        public override Task Execute()
        {
            if (_marginSettings.MaxMarketMakerLimitOrderAge <= 0)
                return Task.CompletedTask;
            
            var maxQuoteAgeInSeconds = _marginSettings.MaxMarketMakerLimitOrderAge;
            
            var now = _dateService.Now();
            var minQuoteDateTime = now.AddSeconds(-maxQuoteAgeInSeconds);
            var minNotificationRepeatDate = now.AddSeconds(-maxQuoteAgeInSeconds * NotificationRepeatTimeoutCoef);
            
            var quotes = _quoteCacheService.GetAllQuotes();
            
            foreach (var quote in quotes)
            {
                if (_dayOffService.IsAssetTradingDisabled(quote.Key))
                    continue;
                
                if (quote.Value.Date <= minQuoteDateTime)
                {
                    if (_outdatedQuotes.TryGetValue(quote.Key, out var info))
                    {
                        if (info.LastNotificationSend < minNotificationRepeatDate)
                        {
                            NotifyQuoteIsOutdated(quote.Value);
                        }
                    }
                    else
                    {
                        NotifyQuoteIsOutdated(quote.Value);
                    }
                }
                else
                {
                    if (_outdatedQuotes.ContainsKey(quote.Key))
                    {
                        NotifyQuoteIsOk(quote.Value);
                    }
                }
            }
            
            return Task.CompletedTask;
        }

        private void NotifyQuoteIsOutdated(InstrumentBidAskPair quote)
        {
            var message = $"Quotes for {quote.Instrument} stopped at {quote.Date}!";
            WriteMessage(quote, message, EventTypeEnum.QuoteStopped);
            var info = new OutdatedQuoteInfo
            {
                LastQuoteRecieved = quote.Date,
                LastNotificationSend = _dateService.Now()
            };
            
            _outdatedQuotes[quote.Instrument] = info;
        }

        private void NotifyQuoteIsOk(InstrumentBidAskPair quote)
        {
            var message = $"Quotes for {quote.Instrument} started at {quote.Date}";
            WriteMessage(quote, message, EventTypeEnum.QuoteStarted);
            _outdatedQuotes.Remove(quote.Instrument);
        }

        private void WriteMessage(InstrumentBidAskPair quote, string message, EventTypeEnum eventType)
        {
            _log.WriteInfoAsync(nameof(QuotesMonitor), quote.ToJson(), message);
        }

        private class OutdatedQuoteInfo
        {
            public DateTime LastQuoteRecieved { get; set; }
            public DateTime LastNotificationSend { get; set; }
        }
    }
}