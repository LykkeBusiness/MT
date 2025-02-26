using System;

using Common;
using Common.Log;

using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services.Snapshots;

internal sealed class LoggingSnapshotRequestQueue(
    ISnapshotRequestQueue decoratee,
    ILog logger) : ISnapshotRequestQueue
{
    public void Acknowledge(Guid requestId)
    {
        decoratee.Acknowledge(requestId);
        logger.WriteInfo(
            nameof(LoggingSnapshotRequestQueue),
            nameof(Acknowledge),
            $"Acknowledged snapshot request {requestId.ToString()}");
    }

    public SnapshotQueueState CaptureState()
    {
        throw new NotImplementedException();
    }

    public SnapshotCreationRequest Dequeue()
    {
        var request = decoratee.Dequeue();
        logger.WriteInfo(
            nameof(LoggingSnapshotRequestQueue),
            nameof(Dequeue),
            $"Dequeued snapshot request: {request.ToJson()}");
        return request;
    }

    public void Enqueue(SnapshotCreationRequest request)
    {
        decoratee.Enqueue(request);
        logger.WriteInfo(
            nameof(LoggingSnapshotRequestQueue),
            nameof(Enqueue),
            $"Enqueued snapshot request: {request.ToJson()}");
    }

    public void RestoreState(SnapshotQueueState state)
    {
        throw new NotImplementedException();
    }
}
