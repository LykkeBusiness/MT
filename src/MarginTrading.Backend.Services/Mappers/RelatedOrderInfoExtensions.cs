// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Common.Extensions;

using OrderStatusContract = MarginTrading.Backend.Contracts.Orders.OrderStatusContract;

namespace MarginTrading.Backend.Services.Mappers
{
    public static class RelatedOrderInfoExtensions
    {
        public static RelatedOrderInfoContract Map(this Order order) => order switch
        {
            null => null,
            _ => new RelatedOrderInfoContract
            {
                Id = order.Id,
                Price = order.Price ?? 0,
                Type = order.OrderType.ToType<OrderTypeContract>(),
                Status = order.Status.ToType<OrderStatusContract>(),
                ModifiedTimestamp = order.LastModified,
                TrailingDistance = order.TrailingDistance
            }
        };
    }
}