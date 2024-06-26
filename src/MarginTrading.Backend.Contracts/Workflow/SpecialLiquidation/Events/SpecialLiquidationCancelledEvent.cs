﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events
{
    /// <summary>
    /// The special liquidation flow cancelled.
    /// </summary>
    [MessagePackObject]
    public class SpecialLiquidationCancelledEvent
    {
        /// <summary>
        /// Operation Id
        /// </summary>
        [Key(0)]
        public string OperationId { get; set; }
        
        /// <summary>
        /// Event creation time
        /// </summary>
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        /// <summary>
        /// Reason of failure
        /// </summary>
        [Key(2)]
        public string Reason { get; set; }
        
        /// <summary>
        /// If positions needs to be closed within regular flow
        /// </summary>
        [Key(3)]
        public bool ClosePositions { get; set; }
    }
}