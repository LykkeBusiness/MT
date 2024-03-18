// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services.Infrastructure
{
    /// <summary>
    /// This class decouples draft snapshot creation from rabbit mq
    /// Normal flow: MarketStateChangedEvent generated => handled in PlatformClosureProjection => snapshot saved
    /// Degraded flow: MarketStateChangedEvent generated, snapshot requested => event not received in PlatformClosureProjection => SnapshotMonitorService retries snapshot creation after a timeout 
    /// </summary>
    public class SnapshotMonitor : ISnapshotMonitor
    {
        private readonly SnapshotMonitorSettings _settings;
        private DraftSnapshotState _state = DraftSnapshotState.None;
        private DateTime _timestamp;
        private DateTime _tradingDay;

        public SnapshotMonitor(SnapshotMonitorSettings settings)
        {
            _settings = settings;
        }
        
        // TODO: concurrency / thread safety?
        public void SnapshotRequested(DateTime tradingDay)
        {
            _state = DraftSnapshotState.Requested;
            _timestamp = DateTime.UtcNow;
            _tradingDay = tradingDay;
        }

        public void SnapshotInProgress()
        {
            _state = DraftSnapshotState.InProgress;
        }

        public void SnapshotFinished()
        {
            _state = DraftSnapshotState.None;
            _timestamp = default;
            _tradingDay = default;
        }

        public bool ShouldRetrySnapshot(out DateTime tradingDay)
        {
            var shouldRetry = _state == DraftSnapshotState.Requested &&
                             _timestamp.Add(_settings.SnapshotCreationTimeout) <= DateTime.UtcNow;
            
            tradingDay = shouldRetry ? _tradingDay : default;
            
            return shouldRetry;
        }
        
        private enum DraftSnapshotState
        {
            None,
            Requested,
            InProgress,
        }
    }
}