using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Snapshots;

public sealed class WaitableQueueAdapter<TRequest, TResult>
    : IWaitableRequestQueue<TRequest, TResult> where TRequest : IIdentifiable
{
    private readonly IRequestQueue<TRequest> _adaptee;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<TResult>> _waiters = new();

    public WaitableQueueAdapter(IRequestQueue<TRequest> adaptee)
    {
        _adaptee = adaptee;
    }

    public TRequest Dequeue() => _adaptee.Dequeue();

    public async Task<TResult> EnqueueAndWait(TRequest request, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<TResult>();

        if (!_waiters.TryAdd(request.Id, tcs))
        {
            return await _waiters[request.Id].Task;
        }

        cancellationToken.Register(() =>
        {
            if (_waiters.TryRemove(request.Id, out var pendingTcs))
            {
                pendingTcs.TrySetCanceled(cancellationToken);
            }
        });

        _adaptee.Enqueue(request);

        // todo: think if timeout is required
        return await tcs.Task;
    }

    public void Acknowledge(Guid requestId, TResult result)
    {
        _adaptee.Acknowledge(requestId);

        if (_waiters.TryRemove(requestId, out var tcs))
        {
            tcs.TrySetResult(result);
        }
    }

    public void Reject(Guid requestId, Exception exception)
    {
        _adaptee.Reject(requestId, exception);

        if (_waiters.TryRemove(requestId, out var tcs))
        {
            tcs.TrySetException(exception);
        }
    }
}
