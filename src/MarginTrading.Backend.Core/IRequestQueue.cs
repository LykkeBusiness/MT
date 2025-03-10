using System;

namespace MarginTrading.Backend.Core;


/// <summary>
/// Adding a request to the queue.
/// Interface is intended to be used in a producer-consumer pattern.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IQueueRequestProducer<T>
{
    void Enqueue(T request);
}

/// <summary>
/// Dequeueing a request from the queue with positive or negative acknowledgement.
/// Interface is intended to be used in a producer-consumer pattern.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IQueueRequestConsumer<T>
{
    T Dequeue();
    void Acknowledge(Guid requestId);
    void Reject(Guid requestId, Exception exception);
}

/// <summary>
/// Request queue.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IRequestQueue<T> : IQueueRequestProducer<T>, IQueueRequestConsumer<T>;