﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class RejectMatchingEngine : IMatchingEngineBase
    {
        public string Id => MatchingEngineConstants.Reject;

        public MatchingEngineMode Mode => MatchingEngineMode.MarketMaker;

        public ValueTask<MatchedOrderCollection> MatchOrderAsync(OrderFulfillmentPlan orderFulfillmentPlan,
            OrderModality modality = OrderModality.Regular)
        {
            return new ValueTask<MatchedOrderCollection>(new MatchedOrderCollection());
        }

        public (string externalProviderId, decimal? price) GetBestPriceForOpen(string assetPairId, decimal volume)
        {
            return (null, null);
        }

        public Task MatchMarketOrderForCloseAsync(Position order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            orderProcessed(new MatchedOrderCollection());
            
            return Task.CompletedTask;
        }

        public decimal? GetPriceForClose(string assetPairId, decimal volume, string externalProviderId)
        {
            return null;
        }

        public OrderBook GetOrderBook(string instrument)
        {
            return new OrderBook(instrument);
        }
    }
}