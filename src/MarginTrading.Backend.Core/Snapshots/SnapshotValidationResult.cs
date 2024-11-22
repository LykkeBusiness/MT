// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Snapshots
{
    /// <summary>
    /// Represents result of trading state validation.
    /// </summary>
    public class SnapshotValidationResult
    {
        public bool IsValid => Orders is { IsValid: true }
                               && Positions is { IsValid: true }
                               && Exception == null;

        public ValidationResult<OrderInfo> Orders { get; set; }

        public ValidationResult<PositionInfo> Positions { get; set; }
        
        public string PreviousSnapshotCorrelationId { get; set; }
        
        public IOrderReaderBase Cache { get; set; }

        public SnapshotValidationException Exception { get; set; }
    }
}