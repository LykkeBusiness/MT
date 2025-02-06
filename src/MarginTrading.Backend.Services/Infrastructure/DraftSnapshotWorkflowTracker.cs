// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services.Infrastructure;

/// <inheritdoc />
public class DraftSnapshotWorkflowTracker : IDraftSnapshotWorkflowTracker
{
    public DraftSnapshotWorkflowState Current { get; private set; } = DraftSnapshotWorkflowState.Pending;
    public DateTime RequestedAt { get; private set; }
    public DateTime TradingDay { get; private set; }

    public bool TryRequest(DateTime timestamp, DateTime tradingDay) => Current switch
    {
        DraftSnapshotWorkflowState.Pending => RequestInternal(timestamp, tradingDay),
        _ => false
    };

    public bool TryStart() => Current switch
    {
        DraftSnapshotWorkflowState.Requested => StartInternal(),
        _ => false
    };

    public bool TryReset() => Current switch
    {
        DraftSnapshotWorkflowState.InProgress => ResetInternal(),
        _ => false
    };

    public void HardReset()
    {
        ResetInternal();
    }

    private bool RequestInternal(DateTime timestamp, DateTime tradingDay)
    {
        Current = DraftSnapshotWorkflowState.Requested;
        RequestedAt = timestamp;
        TradingDay = tradingDay;
        return true;
    }

    private bool StartInternal()
    {
        Current = DraftSnapshotWorkflowState.InProgress;
        return true;
    }


    private bool ResetInternal()
    {
        Current = DraftSnapshotWorkflowState.Pending;
        RequestedAt = default;
        TradingDay = default;
        return true;
    }
}
