// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Services
{
    public interface ISnapshotMonitor
    {
        void SnapshotRequested(DateTime tradingDay);
        void SnapshotInProgress();
        void SnapshotFinished();
        bool ShouldRetrySnapshot(out DateTime tradingDay);
    }
}