// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Orders
{
    public interface IOrderReaderBase
    {
        ImmutableArray<Order> GetAllOrders();
        ImmutableArray<Position> GetPositions();
        bool TryGetOrderById(string orderId, out Order order);
    }
}