// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core.DayOffSettings;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.Mappers
{  public class AccountStatsContract
    {
        public string AccountId { get; set; }
        public string BaseAssetId { get; set; }
        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
        public decimal MarginCallLevel { get; set; }
        public decimal StopOutLevel { get; set; }
        public decimal TotalCapital { get; set; }
        public decimal FreeMargin { get; set; }
        public decimal MarginAvailable { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal MarginInit { get; set; }
        public decimal PnL { get; set; }
        public decimal OpenPositionsCount { get; set; }
        public decimal MarginUsageLevel { get; set; }
        public string LegalEntity { get; set; }
    }
    public class BidAskPairRabbitMqContract
    {
        public string Instrument { get; set; }
        public DateTime Date { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsEod { get; set; }
    }
    public static class DomainToRabbitMqContractMapper
    {
        public static BidAskPairRabbitMqContract ToRabbitMqContract(this InstrumentBidAskPair pair, bool isEod)
        {
            return new BidAskPairRabbitMqContract
            {
                Instrument = pair.Instrument,
                Ask = pair.Ask,
                Bid = pair.Bid,
                Date = pair.Date,
                IsEod = isEod ? true : (bool?)null,
            };
        }

        public static AccountStatsContract ToRabbitMqContract(this IMarginTradingAccount account)
        {
            return new AccountStatsContract
            {
                AccountId = account.Id,
                ClientId = account.ClientId,
                TradingConditionId = account.TradingConditionId,
                BaseAssetId = account.BaseAssetId,
                Balance = account.Balance,
                WithdrawTransferLimit = account.WithdrawTransferLimit,
                MarginCallLevel = account.GetMarginCall1Level(),
                StopOutLevel = account.GetStopOutLevel(),
                TotalCapital = account.GetTotalCapital(),
                FreeMargin = account.GetFreeMargin(),
                MarginAvailable = account.GetMarginAvailable(),
                UsedMargin = account.GetUsedMargin(),
                MarginInit = account.GetMarginInit(),
                PnL = account.GetPnl(),
                OpenPositionsCount = account.GetOpenPositionsCount(),
                MarginUsageLevel = account.GetMarginUsageLevel(),
                LegalEntity = account.LegalEntity,
            };
        }

        public static CompiledScheduleTimeIntervalContract ToRabbitMqContract(this CompiledScheduleTimeInterval schedule)
        {
            return new CompiledScheduleTimeIntervalContract
            {
                Schedule = new ScheduleSettingsContract
                {
                    Id = schedule.Schedule.Id,
                    Rank = schedule.Schedule.Rank,
                    IsTradeEnabled = schedule.Schedule.IsTradeEnabled,
                    PendingOrdersCutOff = schedule.Schedule.PendingOrdersCutOff,
                },
                Start = schedule.Start,
                End = schedule.End,
            };
        }
    }
}