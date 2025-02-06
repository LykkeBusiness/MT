// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Settings
{
    public class SnapshotMonitorSettings
    {
        private const int DefaultSnapshotCreationAtempts = 10;
        private const int DefaultMonitoringDelayInSeconds = 30;
        private const int DefaultDelayBeforeFallbackSnapshotInMinutes = 5;

        /// <summary>
        /// Defines the interval between consecutive checks performed by the SnapshotMonitoringService service.
        /// </summary>
        public TimeSpan MonitoringDelay { get; set; } = TimeSpan.FromSeconds(DefaultMonitoringDelayInSeconds);

        /// <summary>
        /// Maximum number of attempts to create a snapshot CONSISTENTLY
        /// </summary>
        public ushort MaxSnapshotCreationAttempts { get; set; } = DefaultSnapshotCreationAtempts;

        /// <summary>
        /// If snapshot is not created after a specified amount of time, creation will be retried
        /// </summary>
        public TimeSpan DelayBeforeFallbackSnapshot { get; set; } =
            TimeSpan.FromMinutes(DefaultDelayBeforeFallbackSnapshotInMinutes);
    }
}