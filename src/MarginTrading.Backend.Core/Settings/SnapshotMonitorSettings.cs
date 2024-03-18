// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Settings
{
    public class SnapshotMonitorSettings
    {
        /// <summary>
        /// Defines the interval between consecutive checks performed by the SnapshotMonitoring service.
        /// </summary>
        public TimeSpan MonitoringDelay { get; set; }
        
        /// <summary>
        /// If snapshot is not created after a specified amount of time, SnapshotMonitorService will retry this operation
        /// </summary>

        public TimeSpan SnapshotCreationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}