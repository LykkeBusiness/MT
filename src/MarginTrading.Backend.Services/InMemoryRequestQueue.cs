using System;
using System.Collections.Generic;

using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services;

public sealed class InMemoryRequestQueue<T> : IRequestQueue<T> where T : class, IIdentifiable
{
    private readonly object _lock = new();
    private readonly Queue<T> _queue = new();
    private T _inFlight;

    public RequestQueueState<T> State => new([.. _queue], _inFlight);

    public void Acknowledge(Guid requestId)
    {
        lock (_lock)
        {
            if (_inFlight is not null && _inFlight.Id == requestId)
            {
                _inFlight = null;
                return;
            }

            throw new InvalidOperationException("Request in processing does not match the provided id to ack.");
        }
    }

    public void Reject(Guid requestId, Exception exception)
    {
        lock (_lock)
        {
            if (_inFlight is not null && _inFlight.Id == requestId)
            {
                _inFlight = null;
                return;
            }

            throw new InvalidOperationException("Request in processing does not match the provided id to reject.");
        }
    }

    public void Enqueue(T request)
    {
        lock (_lock)
        {
            _queue.Enqueue(request);
        }
    }

    public T Dequeue()
    {
        lock (_lock)
        {
            if (_inFlight is not null)
            {
                throw new InvalidOperationException("There is already a request in processing. Acknowledge or reject it first.");
            }

            if (_queue.Count == 0)
            {
                return null;
            }

            _inFlight = _queue.Dequeue();
            return _inFlight;
        }
    }
}
