// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Settings;

public class SnapshotMonitorSettings
{
    private const int DefaultMonitoringDelayInSeconds = 30;

    /// <summary>
    /// Defines the interval between consecutive checks performed by the SnapshotMonitoringService service.
    /// </summary>
    public TimeSpan MonitoringDelay { get; set; } = TimeSpan.FromSeconds(DefaultMonitoringDelayInSeconds);
}