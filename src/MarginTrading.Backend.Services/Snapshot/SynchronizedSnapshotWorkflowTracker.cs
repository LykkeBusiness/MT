// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services.Snapshot;

/// <summary>
/// Synchronized draft snapshot workflow tracker, thread-safe.
/// </summary>
public class SynchronizedSnapshotWorkflowTracker : IDraftSnapshotWorkflowTracker
{
    private readonly object _sync = new();
    private readonly IDraftSnapshotWorkflowTracker _decoratee;

    public SynchronizedSnapshotWorkflowTracker(IDraftSnapshotWorkflowTracker decoratee)
    {
        _decoratee = decoratee;
    }

    public DraftSnapshotWorkflowState Current
    {
        get { lock (_sync) { return _decoratee.Current; } }
    }

    public DateTime RequestedAt
    {
        get { lock (_sync) { return _decoratee.RequestedAt; } }
    }

    public DateTime TradingDay
    {
        get { lock (_sync) { return _decoratee.TradingDay; } }
    }

    public bool TryRequest(DateTime timestamp, DateTime tradingDay)
    {
        lock (_sync)
        {
            return _decoratee.TryRequest(timestamp, tradingDay);
        }
    }

    public bool TryReset()
    {
        lock (_sync)
        {
            return _decoratee.TryReset();
        }
    }

    public bool TryStart()
    {
        lock (_sync)
        {
            return _decoratee.TryStart();
        }
    }

    public void HardReset()
    {
        lock (_sync)
        {
            _decoratee.HardReset();
        }
    }
}
