// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Snapshots
{
    /// <summary>
    /// Represents result of trading state validation.
    /// </summary>
    public class SnapshotValidationResult
    {
        public bool IsValid => Orders.IsValid && Positions.IsValid;

        public ValidationResult<OrderInfo> Orders { get; set; }

        public ValidationResult<PositionInfo> Positions { get; set; }
        
        public string PreviousSnapshotCorrelationId { get; set; }
        
        public IOrderReader Cache { get; set; }
    }
}