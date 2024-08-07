﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.RabbitMqMessageModels;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core.DayOffSettings;

namespace MarginTrading.Backend.Core.Mappers
{
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