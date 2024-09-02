// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.ExchangeConnector;
using MarginTrading.Backend.Contracts.RabbitMqMessageModels;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Notifications;

namespace MarginTradingTests
{
    public class PositionHistoryNotifications : IRabbitMqNotifyService
    {
        private readonly List<PositionHistoryEvent> _container;

        public PositionHistoryNotifications(List<PositionHistoryEvent> container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }
        public Task OrderHistory(Order order, OrderUpdateType orderUpdateType, string activitiesMetadata = null)
        {
            throw new NotImplementedException();
        }

        public Task OrderBookPrice(InstrumentBidAskPair quote, bool isEod)
        {
            throw new NotImplementedException();
        }

        public Task AccountMarginEvent(MarginEventMessage eventMessage)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAccountStats(AccountStatsUpdateMessage message)
        {
            throw new NotImplementedException();
        }

        public Task NewTrade(TradeContract trade)
        {
            throw new NotImplementedException();
        }

        public Task ExternalOrder(ExecutionReport trade)
        {
            throw new NotImplementedException();
        }

        public Task PositionHistory(PositionHistoryEvent historyEvent)
        {
            _container.Add(historyEvent);
            return Task.CompletedTask;
        }

        public Task Rfq(RfqEvent rfqEvent)
        {
            throw new NotImplementedException();
        }
    }
}