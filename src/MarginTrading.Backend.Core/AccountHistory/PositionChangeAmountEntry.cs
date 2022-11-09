// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.AccountHistory
{
    public readonly struct PositionChangeAmountEntry
    {
        public PositionChangeAmountEntry(int oid, string positionId, decimal changeAmount, string operationId)
        {
            if (string.IsNullOrEmpty(positionId))
                throw new ArgumentNullException(nameof(positionId));
            
            PositionId = positionId;
            ChangeAmount = changeAmount;
            OperationId = operationId;
            Oid = oid;
        }

        public int Oid { get; } 
        public string PositionId { get; }
        public decimal ChangeAmount { get; }
        public string OperationId { get; }
            
    }
}