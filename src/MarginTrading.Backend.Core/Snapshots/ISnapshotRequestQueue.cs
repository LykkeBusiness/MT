using System;

namespace MarginTrading.Backend.Core.Snapshots;

public interface ISnapshotRequestQueue
{
    void Enqueue(SnapshotCreationRequest request);
    SnapshotCreationRequest Dequeue();
    void Acknowledge(Guid requestId);
    SnapshotQueueState CaptureState();
    void RestoreState(SnapshotQueueState state);
}