﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.MatchedOrders
{
    [JsonConverter(typeof(MatchedOrderCollectionConverter))]
    public class MatchedOrderCollection : IReadOnlyCollection<MatchedOrder>
    {
        private ImmutableList<MatchedOrder> _items;

        public decimal SummaryVolume { get; private set; }
        public decimal WeightedAveragePrice { get; private set; }

        public ImmutableList<MatchedOrder> Items
        {
            get => _items;
            private set
            {
                _items = value;

                SummaryVolume = _items.Sum(item => Math.Abs(item.Volume));

                if (SummaryVolume > 0)
                {
                    WeightedAveragePrice = _items.Sum(x => x.Price * Math.Abs(x.Volume)) / SummaryVolume;
                }
            }
        }

        public MatchedOrderCollection(IEnumerable<MatchedOrder> orders = null)
        {
            Items = orders?.ToImmutableList() ?? [];
        }

        public void Add(MatchedOrder order)
        {
            Items = Items.Add(order);
        }

        public void AddRange(IEnumerable<MatchedOrder> orders)
        {
            Items = Items.AddRange(orders);
        }

        #region IReadOnlyCollection

        public IEnumerator<MatchedOrder> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int Count => _items.Count;

        #endregion

    }
}