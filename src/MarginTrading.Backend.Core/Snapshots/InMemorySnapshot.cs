// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Snapshots
{
    public class InMemorySnapshot : IOrderReader
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

        public ImmutableArray<Position> GetPositions(string instrument)
        {
            return _positions.Where(x => x.AssetPairId == instrument).ToImmutableArray();
        }

        public ImmutableArray<Position> GetPositionsByFxAssetPairId(string fxAssetPairId)
        {
            return _positions.Where(x => x.FxAssetPairId == fxAssetPairId).ToImmutableArray();
        }

        public ImmutableArray<Order> GetPending()
        {
            return _orders.Where(x => x.Status == OrderStatus.Active).ToImmutableArray();
        }

        public bool TryGetOrderById(string orderId, out Order order)
        {
            order = _orders.FirstOrDefault(x => x.Id == orderId);
            return order != null;
        }
    }
}