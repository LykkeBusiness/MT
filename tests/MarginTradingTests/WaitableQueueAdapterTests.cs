namespace MarginTradingTests;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Snapshots;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class FakeRequestQueue<T> : IRequestQueue<T> where T : class, IIdentifiable
{
    private readonly Queue<T> _queue = new();
    private T _inFlight;

    public RequestQueueState<T> State => new([.. _queue], _inFlight);

    public int AcknowledgeCallCount { get; private set; } = 0;
    public int RejectCallCount { get; private set; } = 0;
    public int EnqueueCallCount { get; private set; } = 0;
    public int DequeueCallCount { get; private set; } = 0;

    public void Acknowledge(Guid requestId)
    {
        AcknowledgeCallCount++;
        if (_inFlight != null && _inFlight.Id == requestId)
        {
            _inFlight = null;
            return;
        }
        throw new InvalidOperationException("Request in processing does not match the provided id to ack.");
    }

    public T Dequeue()
    {
        DequeueCallCount++;
        if (_inFlight != null)
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

    public void Enqueue(T request)
    {
        EnqueueCallCount++;
        _queue.Enqueue(request);
    }

    public void Reject(Guid requestId, Exception exception)
    {
        RejectCallCount++;
        if (_inFlight != null && _inFlight.Id == requestId)
        {
            _inFlight = null;
            return;
        }
        throw new InvalidOperationException("Request in processing does not match the provided id to reject.");
    }
}

[TestFixture]
public class WaitableQueueAdapterTests
{
    private FakeRequestQueue<TestRequest> _innerQueue;
    private WaitableQueueAdapter<TestRequest, string> _waitableQueue;

    [SetUp]
    public void Setup()
    {
        _innerQueue = new FakeRequestQueue<TestRequest>();
        _waitableQueue = new WaitableQueueAdapter<TestRequest, string>(_innerQueue);
    }

    [Test]
    public void Dequeue_DelegatesCallToAdaptee()
    {
        var originalRequest = new TestRequest("test data");
        _innerQueue.Enqueue(originalRequest);

        var dequeuedRequest = _waitableQueue.Dequeue();

        Assert.That(_innerQueue.DequeueCallCount, Is.EqualTo(1));
        Assert.That(dequeuedRequest, Is.EqualTo(originalRequest));
    }

    [Test]
    public async Task EnqueueAndWait_EnqueuesRequestAndReturnsResult_WhenAcknowledged()
    {
        var originalRequest = new TestRequest("test data");
        var expectedResult = "processed result";

        var waitTask = _waitableQueue.EnqueueAndWait(originalRequest);

        // Simulate processing in another thread
        var dequeuedRequest = _waitableQueue.Dequeue();
        await Task.Delay(10);
        _waitableQueue.Acknowledge(dequeuedRequest.Id, expectedResult);

        var result = await waitTask;

        Assert.That(_innerQueue.EnqueueCallCount, Is.EqualTo(1));
        Assert.That(_innerQueue.DequeueCallCount, Is.EqualTo(1));
        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public async Task EnqueueAndWait_EnqueuesRequestAndReturns_WhenRejected()
    {
        var originalRequest = new TestRequest("test data");
        var expectedException = new InvalidOperationException("Test exception");

        var _ = _waitableQueue.EnqueueAndWait(originalRequest);

        // Simulate processing in another thread
        var dequeuedRequest = _waitableQueue.Dequeue();
        await Task.Delay(10);
        _waitableQueue.Reject(dequeuedRequest.Id, expectedException);

        Assert.That(_innerQueue.EnqueueCallCount, Is.EqualTo(1));
        Assert.That(_innerQueue.DequeueCallCount, Is.EqualTo(1));
        Assert.That(_innerQueue.RejectCallCount, Is.EqualTo(1));
    }

    [Test]
    public void EnqueueAndWait_ReturnsCancelledTask_WhenCancelled()
    {
        var request = new TestRequest("test data");
        var cts = new CancellationTokenSource();

        var waitTask = _waitableQueue.EnqueueAndWait(request, cts.Token);

        cts.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(() => waitTask);
        Assert.That(_innerQueue.EnqueueCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task EnqueueAndWait_ReturnsSameTask_WhenRequestIdAlreadyExists()
    {
        var originalRequest = new TestRequest("test data");
        var expectedResult = "processed result";

        var waitTask1 = _waitableQueue.EnqueueAndWait(originalRequest);
        var waitTask2 = _waitableQueue.EnqueueAndWait(originalRequest);

        // Simulate processing in another thread
        var dequeuedRequest = _waitableQueue.Dequeue();
        await Task.Delay(10);
        _waitableQueue.Acknowledge(dequeuedRequest.Id, expectedResult);

        var result1 = await waitTask1;
        var result2 = await waitTask2;

        Assert.That(_innerQueue.EnqueueCallCount, Is.EqualTo(1), "Request should be enqueued only once");
        Assert.That(result1, Is.EqualTo(expectedResult));
        Assert.That(result2, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Acknowledge_DelegatesCallToAdaptee()
    {
        var originalRequest = new TestRequest("test data");
        var result = "processed result";
        _innerQueue.Enqueue(originalRequest);
        var dequeuedRequest = _innerQueue.Dequeue();

        _waitableQueue.Acknowledge(dequeuedRequest.Id, result);

        Assert.That(_innerQueue.AcknowledgeCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Acknowledge_CompletesWaiterTaskWithResult_WhenWaiterExists()
    {
        var originalRequest = new TestRequest("test data");
        var expectedResult = "processed result";
        var waitTask = _waitableQueue.EnqueueAndWait(originalRequest);

        var dequeuedRequest = _waitableQueue.Dequeue();
        _waitableQueue.Acknowledge(dequeuedRequest.Id, expectedResult);
        var result = await waitTask;

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Reject_DelegatesCallToAdaptee()
    {
        var originalRequest = new TestRequest("test data");
        var exception = new Exception("Test exception");
        _innerQueue.Enqueue(originalRequest);

        var dequeuedRequest = _innerQueue.Dequeue();
        _waitableQueue.Reject(dequeuedRequest.Id, exception);

        Assert.That(_innerQueue.AcknowledgeCallCount, Is.EqualTo(0));
        Assert.That(_innerQueue.RejectCallCount, Is.EqualTo(1));
    }

    [Test]
    public void Reject_CompletesWaiterTaskWithException_WhenWaiterExists()
    {
        var originalRequest = new TestRequest("test data");
        var expectedException = new InvalidOperationException("Test exception");
        var waitTask = _waitableQueue.EnqueueAndWait(originalRequest);

        var dequeuedRequest = _waitableQueue.Dequeue();
        _waitableQueue.Reject(dequeuedRequest.Id, expectedException);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => waitTask);
        Assert.That(ex.Message, Is.EqualTo("Test exception"));
    }

    [Test]
    public async Task MultipleRequests_ProcessedIndependently()
    {
        var request1 = new TestRequest("test data 1");
        var request2 = new TestRequest("test data 2");
        var request3 = new TestRequest("test data 3");

        var result1 = "result 1";
        var result2 = "result 2";
        var exception3 = new InvalidOperationException("Error for request 3");

        var task1 = _waitableQueue.EnqueueAndWait(request1);
        var task2 = _waitableQueue.EnqueueAndWait(request2);
        var task3 = _waitableQueue.EnqueueAndWait(request3);

        _waitableQueue.Dequeue();
        _waitableQueue.Acknowledge(request1.Id, result1);
        _waitableQueue.Dequeue();
        _waitableQueue.Acknowledge(request2.Id, result2);
        _waitableQueue.Dequeue();
        _waitableQueue.Reject(request3.Id, exception3);

        var actualResult1 = await task1;
        var actualResult2 = await task2;

        Assert.That(actualResult1, Is.EqualTo(result1));
        Assert.That(actualResult2, Is.EqualTo(result2));
        Assert.ThrowsAsync<InvalidOperationException>(() => task3);
    }
}