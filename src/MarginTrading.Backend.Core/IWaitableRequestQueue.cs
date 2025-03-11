using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core;

/// <summary>
/// Adding a request to the queue and waiting for the result.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResult"></typeparam>
public interface IWaitableRequestProducer<TRequest, TResult> where TRequest : IIdentifiable
{
    Task<TResult> EnqueueAndWait(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dequeueing a request from the queue with positive or negative acknowledgement.
/// Interface is intended to be used in a producer-consumer pattern.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResult"></typeparam>
public interface IWaitableRequestConsumer<TRequest, TResult> where TRequest : IIdentifiable
{
    TRequest Dequeue();
    void Acknowledge(Guid requestId, TResult result);
    void Reject(Guid requestId, Exception exception);
}

/// <summary>
/// Request queue with waitable requests.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResult"></typeparam>
public interface IWaitableRequestQueue<TRequest, TResult>
    : IWaitableRequestProducer<TRequest, TResult>, IWaitableRequestConsumer<TRequest, TResult>
    where TRequest : IIdentifiable;