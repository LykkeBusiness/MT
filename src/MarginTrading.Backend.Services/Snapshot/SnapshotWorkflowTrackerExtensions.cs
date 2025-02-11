// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using MarginTrading.Backend.Core.Services;

namespace MarginTrading.Backend.Services.Snapshot;

internal static class SnapshotWorkflowExtensions
{
    /// <summary>
    /// Checks if the time has passed since the snapshot was requested
    /// </summary>
    /// <param name="tracker"></param>
    /// <param name="time">Period of time to check</param>
    /// <param name="currentTime"></param>
    /// <returns></returns>
    public static bool IsTimePassed(this IDraftSnapshotWorkflowTracker tracker, TimeSpan time, DateTime currentTime) =>
        tracker.RequestedAt.Add(time) <= currentTime;
}