// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Caches;

public static class OrderReaderBaseExtensions
{
    public static IEnumerable<Order> GetRelatedOrders(this IOrderReaderBase reader, Order order)
    {
        foreach (var relatedOrderInfo in order.RelatedOrders)
        {
            if (reader.TryGetOrderById(relatedOrderInfo.Id, out var relatedOrder))
                yield return relatedOrder;
        }
    }

    public static IEnumerable<Order> GetRelatedOrders(this IOrderReaderBase reader, Position position)
    {
        foreach (var relatedOrderInfo in position.RelatedOrders)
        {
            if (reader.TryGetOrderById(relatedOrderInfo.Id, out var relatedOrder))
                yield return relatedOrder;
        }
    }

    public static ImmutableDictionary<string, ImmutableArray<Order>> GetRelatedOrders(
        this IOrderReaderBase reader,
        IEnumerable<Order> orders) =>
        orders.ToImmutableDictionary(o => o.Id, o => reader.GetRelatedOrders(o).ToImmutableArray());

    public static ImmutableDictionary<string, ImmutableArray<Order>> GetRelatedOrders(
        this IOrderReaderBase reader,
        IEnumerable<Position> positions) =>
        positions.ToImmutableDictionary(p => p.Id, p => reader.GetRelatedOrders(p).ToImmutableArray());
}