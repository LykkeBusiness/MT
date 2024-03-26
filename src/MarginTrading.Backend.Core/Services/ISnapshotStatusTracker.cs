// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Services
{
    /// <summary>
    /// This status tracker decouples draft snapshot creation from rabbit mq
    /// Normal flow: <see cref="MarginTrading.Backend.Contracts.TradingSchedule.MarketStateChangedEvent" /> generated => handled in PlatformClosureProjection => snapshot saved
    /// Degraded flow: <see cref="MarginTrading.Backend.Contracts.TradingSchedule.MarketStateChangedEvent" /> generated, snapshot requested => event not received in PlatformClosureProjection => SnapshotMonitoringService retries snapshot creation after a timeout 
    /// </summary>
    public interface ISnapshotStatusTracker
    {
        void SnapshotRequested(DateTime tradingDay);
        void SnapshotInProgress();
        void SnapshotCreated();
        bool ShouldRetrySnapshot(out DateTime tradingDay);
    }
}