// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Orders
{
    /// <summary>
    /// The data structure which contains details on possible order fulfillment 
    /// </summary>
    public sealed class OrderFulfillmentPlan
    {
        /// <summary>
        /// The order, fulfillment plan is linked to
        /// </summary>
        public Order Order { get; }

        /// <summary>
        /// The order volume left to fulfill, considering it will be fully or partially
        /// fulfilled using positions in opposite direction 
        /// </summary>
        public decimal UnfulfilledVolume {
            get
            {
                var oppositeVolume = Math.Abs(OppositePositionsState?.Volume ?? 0);

                if (oppositeVolume >= Math.Abs(Order.Volume))
                {
                    return 0;
                }
                
                return Order.Volume >= 0
                    ? Order.Volume - oppositeVolume
                    : Order.Volume + oppositeVolume;
            } 
        }

        /// <summary>
        /// Designates if new position has to be opened to fulfill the order
        /// </summary>
        public bool RequiresPositionOpening { get; }
        
        /// <summary>
        /// Designates if the order will open short position
        /// </summary>
        public bool WillOpenShortPosition => RequiresPositionOpening && Order.Direction == OrderDirection.Sell;

        /// <summary>
        /// Opposite direction matched positions state
        /// </summary>
        [CanBeNull]
        public MatchedPositionsState OppositePositionsState { get; }

        private OrderFulfillmentPlan(Order order,
            bool requiresPositionOpening)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
            RequiresPositionOpening = requiresPositionOpening;
        }

        private OrderFulfillmentPlan(Order order, MatchedPositionsState oppositePositionsState)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
            OppositePositionsState = oppositePositionsState;

            RequiresPositionOpening = Math.Abs(UnfulfilledVolume) > 0;
        }

        /// <summary>
        /// Creates the fulfillment plan regardless of any other conditions
        /// </summary>
        /// <param name="order">The order</param>
        /// <param name="requiresPositionOpening">Indicates if new position should be opened or not</param>
        /// <returns></returns>
        public static OrderFulfillmentPlan Force(Order order, bool requiresPositionOpening) =>
            new OrderFulfillmentPlan(order, requiresPositionOpening);

        /// <summary>
        /// Creates the fulfillment plan based on already opened matched positions volume math
        /// </summary>
        /// <param name="order">The order</param>
        /// <param name="openedPositions">Already opened positions in opposite direction</param>
        /// <returns></returns>
        public static OrderFulfillmentPlan Create(Order order,
            ICollection<Position> openedPositions)
        {
            var sameAssetRequirement = openedPositions.All(p => p.AssetPairId == order.AssetPairId);
            if (!sameAssetRequirement)
                throw new OrderFulfillmentPlanException("All positions must match order asset");

            var statusRequirement = openedPositions.All(p => p.Status == PositionStatus.Active);
            if (!statusRequirement)
                throw new OrderFulfillmentPlanException("All positions must be active");

            var directionRequirement =
                openedPositions.All(p => p.Direction == order.Direction.GetClosePositionDirection());
            if (!directionRequirement)
                throw new OrderFulfillmentPlanException("All positions must be in opposite direction");

            var accountRequirement = openedPositions.All(p => p.AccountId == order.AccountId);
            if (!accountRequirement)
                throw new OrderFulfillmentPlanException("All positions must match order account");

            var summary = openedPositions.SummarizeVolume();

            return new OrderFulfillmentPlan(order, new MatchedPositionsState(order.Id, summary.Margin, summary.Volume));
        }

        /// <summary>
        /// Creates the fulfillment plan based on already opened matched position volume math
        /// </summary>
        public static OrderFulfillmentPlan Create(Order order, Position position) =>
            Create(order, new List<Position> { position });
    }
}