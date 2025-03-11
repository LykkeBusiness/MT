using System;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Snapshots;

internal sealed class LoggingSnapshotRequestQueue(
    IRequestQueue<SnapshotCreationRequest> decoratee,
    ILogger<LoggingSnapshotRequestQueue> logger) : IRequestQueue<SnapshotCreationRequest>
{
    public void Acknowledge(Guid requestId)
    {
        decoratee.Acknowledge(requestId);
        logger.LogInformation("Acknowledged snapshot request {RequestId}", requestId);
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

    public void Reject(Guid requestId, Exception exception)
    {
        decoratee.Reject(requestId, exception);
        logger.LogError(exception, "Rejected snapshot request {RequestId}", requestId);
    }
}
