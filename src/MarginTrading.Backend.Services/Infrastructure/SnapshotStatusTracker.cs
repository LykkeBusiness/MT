// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services.Infrastructure
{
    /// <inheritdoc />
    public class SnapshotStatusTracker : ISnapshotStatusTracker
    {
        private readonly SnapshotMonitorSettings _settings;
        private DraftSnapshotStatus _status = DraftSnapshotStatus.None;
        private DateTime _timestamp;
        private DateTime _tradingDay;

        public SnapshotStatusTracker(SnapshotMonitorSettings settings)
        {
            _settings = settings;
        }
        
        // TODO: concurrency / thread safety?
        public void SnapshotRequested(DateTime tradingDay)
        {
            _status = DraftSnapshotStatus.Requested;
            _timestamp = DateTime.UtcNow;
            _tradingDay = tradingDay;
        }

        public void SnapshotInProgress()
        {
            if (_status != DraftSnapshotStatus.Requested)
            {
                return;
            }
            _status = DraftSnapshotStatus.InProgress;
        }

        public void SnapshotCreated()
        {
            if (_status != DraftSnapshotStatus.InProgress)
            {
                return;
            }
            _status = DraftSnapshotStatus.None;
            _timestamp = default;
            _tradingDay = default;
        }

        public bool ShouldRetrySnapshot(out DateTime tradingDay)
        {
            var shouldRetry = _status == DraftSnapshotStatus.Requested &&
                             _timestamp.Add(_settings.DelayBeforeFallbackSnapshot) <= DateTime.UtcNow;
            
            tradingDay = shouldRetry ? _tradingDay : default;
            
            return shouldRetry;
        }
        
        private enum DraftSnapshotStatus
        {
            None,
            Requested,
            InProgress,
        }
    }
}