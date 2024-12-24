// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services.Events;

using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.Liquidation.Commands
{
    [MessagePackObject]
    public class StartLiquidationInternalCommand
    {
        [Key(0)]
        public string OperationId { get; set; }

        [Key(1)]
        public DateTime CreationTime { get; set; }

        [Key(2)]
        public string AccountId { get; set; }

        [Key(3)]
        public string AssetPairId { get; set; }

        [Key(4)]
        public PositionDirection? Direction { get; set; }

        [Key(5)]
        public string QuoteInfo { get; set; }

        [Key(6)]
        public LiquidationType LiquidationType { get; set; }

        [Key(7)]
        public OriginatorType OriginatorType { get; set; }

        [Key(8)]
        public string AdditionalInfo { get; set; }

        [Key(9)]
        public string AccountMetadata { get; set; }
    }
}