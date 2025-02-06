// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Core.Services
{
    /// <summary>
    /// This workflow state tracker decouples draft snapshot creation from rabbit mq
    /// Normal flow: 
    ///     <see cref="Contracts.TradingSchedule.MarketStateChangedEvent" />  generated => 
    ///     handled in PlatformClosureProjection => 
    ///     snapshot saved
    /// Degraded flow: 
    ///     <see cref="Contracts.TradingSchedule.MarketStateChangedEvent" /> generated, snapshot requested => 
    ///     event not received in PlatformClosureProjection => 
    ///     SnapshotMonitoringService retries snapshot creation after a timeout 
    /// </summary>
    public interface IDraftSnapshotWorkflowTracker
    {
        /// <summary>
        /// Tries to change the workflow state to "Requested"
        /// </summary>
        /// <param name="timestamp">The time when the snapshot was requested</param>
        /// <param name="tradingDay">The trading day for which the snapshot is requested</param>
        /// <returns>
        /// True if the request was successful, false otherwise
        /// </returns>
        bool TryRequest(DateTime timestamp, DateTime tradingDay);

        /// <summary>
        /// Tries to change the workflow state to "Started"
        /// </summary>
        /// <returns>
        /// True if the request was successful, false otherwise
        /// </returns>
        bool TryStart();

        /// <summary>
        /// Tries to change the workflow state back to initial "Pending"
        /// </summary>
        /// <returns>
        /// True if the request was successful, false otherwise
        /// </returns>
        bool TryReset();

        /// <summary>
        /// Changes the workflow state to "Pending" without any checks
        /// </summary>
        void HardReset();

        DraftSnapshotWorkflowState Current { get; }

        DateTime RequestedAt { get; }

        DateTime TradingDay { get; }
    }
}