// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Settings
{
    public class SnapshotMonitorSettings
    {
        /// <summary>
        /// Defines the interval between consecutive checks performed by the SnapshotMonitoringService service.
        /// </summary>
        public TimeSpan MonitoringDelay { get; set; } = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// If snapshot is not created after a specified amount of time, creation will be retried
        /// </summary>
        public TimeSpan DelayBeforeFallbackSnapshot { get; set; } = TimeSpan.FromMinutes(5);
    }
}