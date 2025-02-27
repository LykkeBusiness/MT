using System;

using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Snapshots;

internal sealed class LoggingSnapshotRequestQueue(
    ISnapshotRequestQueue decoratee,
    ILogger<LoggingSnapshotRequestQueue> logger) : ISnapshotRequestQueue
{
    public void Acknowledge(Guid requestId)
    {
        decoratee.Acknowledge(requestId);
        logger.LogInformation("Acknowledged snapshot request {RequestId}", requestId);
    }

    public SnapshotQueueState CaptureState()
    {
        throw new NotImplementedException();
    }

    public SnapshotCreationRequest Dequeue()
    {
        var request = decoratee.Dequeue();
        logger.LogInformation("Dequeued snapshot request: {@Request}", request);
        return request;
    }

    public void Enqueue(SnapshotCreationRequest request)
    {
        decoratee.Enqueue(request);
        logger.LogInformation("Enqueued snapshot request: {@Request}", request);
    }

    public void RestoreState(SnapshotQueueState state)
    {
        throw new NotImplementedException();
    }
}
