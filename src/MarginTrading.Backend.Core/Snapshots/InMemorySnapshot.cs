// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Snapshots
{
    public class InMemorySnapshot : IOrderReaderBase
    {
        private readonly ImmutableArray<Order> _orders;
        private readonly ImmutableArray<Position> _positions;

        public InMemorySnapshot(ImmutableArray<Order> orders, ImmutableArray<Position> positions)
        {
            _orders = orders;
            _positions = positions;
        }
        
        public ImmutableArray<Order> GetAllOrders()
        {
            return _orders;
        }

        public ImmutableArray<Position> GetPositions()
        {
            return _positions;
        }

        public bool TryGetOrderById(string orderId, out Order order)
        {
            order = _orders.FirstOrDefault(x => x.Id == orderId);
            return order != null;
        }
    }
}