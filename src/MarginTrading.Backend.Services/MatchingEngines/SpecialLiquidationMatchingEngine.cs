// Copyright (c) 2019 Lykke Corp.
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
    /// <summary>
    /// Matching Engine for Special Liquidation ONLY!
    /// Instance is created in CommandsHandler and passed to TradingEngine, no IoC registration here. 
    /// </summary>
    public class SpecialLiquidationMatchingEngine : ISpecialLiquidationMatchingEngine
    {
        public string Id => MatchingEngineConstants.DefaultSpecialLiquidation;
        public MatchingEngineMode Mode => MatchingEngineMode.Stp;
        private readonly decimal _price;
        private readonly string _marketMakerId;
        private readonly string _externalOrderId;
        private readonly DateTime _externalExecutionTime;

        public SpecialLiquidationMatchingEngine(
            decimal price,
            string marketMakerId,
            string externalOrderId,
            DateTime externalExecutionTime)
        {
            _price = price;
            _marketMakerId = marketMakerId;
            _externalOrderId = externalOrderId;
            _externalExecutionTime = externalExecutionTime;
        }
        
        public ValueTask<MatchedOrderCollection> MatchOrderAsync(OrderFulfillmentPlan orderFulfillmentPlan,
            OrderModality modality = OrderModality.Regular)
        {
            return new ValueTask<MatchedOrderCollection>(
                new MatchedOrderCollection(new[]
                {
                    new MatchedOrder
                    {
                        OrderId = _externalOrderId,
                        MarketMakerId = _marketMakerId,
                        Volume = Math.Abs(orderFulfillmentPlan.Order.Volume),
                        Price = _price,
                        MatchedDate = _externalExecutionTime,
                        IsExternal = true,
                    }
                }));
        }

        public (string externalProviderId, decimal? price) GetBestPriceForOpen(string assetPairId, decimal volume)
        {
            return (_marketMakerId, _price);
        }

        public decimal? GetPriceForClose(string assetPairId, decimal volume, string externalProviderId)
        {
            return _price;
        }

        public OrderBook GetOrderBook(string instrument)
        {
            throw new NotImplementedException();
        }
    }
}