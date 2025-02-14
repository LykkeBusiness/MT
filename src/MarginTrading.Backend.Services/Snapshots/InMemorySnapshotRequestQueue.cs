using System;
using System.Collections.Generic;

using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services.Snapshots;

public sealed class InMemorySnapshotRequestQueue : ISnapshotRequestQueue
{
    private readonly object _lock = new();
    private readonly Queue<SnapshotCreationRequest> _queue = new();
    private SnapshotCreationRequest _inFlight;

    public void Acknowledge(Guid requestId)
    {
        lock (_lock)
        {
            if (_inFlight is not null && _inFlight.Id == requestId)
            {
                _inFlight = null;
                return;
            }

            throw new InvalidOperationException("Request in processing does not match the provided id.");
        }
    }

    public SnapshotQueueState CaptureState()
    {
        lock (_lock)
        {
            return new SnapshotQueueState(
                [.. _queue],
                _inFlight
            );
        }
    }

    public SnapshotCreationRequest Dequeue()
    {
        lock (_lock)
        {
            if (_inFlight is not null)
            {
                throw new InvalidOperationException("There is already a request in processing. Acknowledge it first.");
            }

            if (_queue.Count == 0)
            {
                return null;
            }

            _inFlight = _queue.Dequeue();
            return _inFlight;
        }
    }

    public void Enqueue(SnapshotCreationRequest request)
    {
        lock (_lock)
        {
            _queue.Enqueue(request);
        }
    }

    public void RestoreState(SnapshotQueueState state)
    {
        lock (_lock)
        {
            if (_queue.Count > 0 || _inFlight is not null)
            {
                throw new InvalidOperationException("Restore is only allowed on an empty queue and no in-flight request");
            }

            if (state.InFlightRequest is not null)
                _queue.Enqueue(state.InFlightRequest);

            foreach (var request in state.PendingRequests)
            {
                _queue.Enqueue(request);
            }
        }
    }
}