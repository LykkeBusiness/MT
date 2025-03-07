namespace MarginTradingTests;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services;

using NUnit.Framework;

using System;
using System.Linq;

[TestFixture]
public class InMemoryRequestQueueTests
{
    class TestRequest(string data) : IIdentifiable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Data { get; } = data;
    }

    private InMemoryRequestQueue<TestRequest> _queue;

    [SetUp]
    public void Setup()
    {
        _queue = new InMemoryRequestQueue<TestRequest>();
    }

    [Test]
    public void State_ReturnsCorrectState_WhenEmpty()
    {
        var state = _queue.State;

        Assert.That(state.PendingRequests, Is.Empty);
        Assert.That(state.InFlightRequest, Is.Null);
    }

    [Test]
    public void Enqueue_AddsItemToQueue()
    {
        var request = new TestRequest("test data");

        _queue.Enqueue(request);
        var state = _queue.State;

        Assert.That(state.PendingRequests, Has.Count.EqualTo(1));
        Assert.That(state.PendingRequests.First(), Is.EqualTo(request));
    }

    [Test]
    public void Enqueue_MultipleItems_AddsAllItemsToQueue()
    {
        var request1 = new TestRequest("test data 1");
        var request2 = new TestRequest("test data 2");
        var request3 = new TestRequest("test data 3");

        _queue.Enqueue(request1);
        _queue.Enqueue(request2);
        _queue.Enqueue(request3);
        var state = _queue.State;

        Assert.That(state.PendingRequests, Has.Count.EqualTo(3));
        Assert.That(state.PendingRequests.ElementAt(0), Is.EqualTo(request1));
        Assert.That(state.PendingRequests.ElementAt(1), Is.EqualTo(request2));
        Assert.That(state.PendingRequests.ElementAt(2), Is.EqualTo(request3));
    }

    [Test]
    public void Dequeue_ReturnsNull_WhenQueueIsEmpty()
    {
        var result = _queue.Dequeue();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Dequeue_ReturnsAndSetsInFlightItem_WhenQueueHasItems()
    {
        var request = new TestRequest("test data");
        _queue.Enqueue(request);

        var result = _queue.Dequeue();
        var state = _queue.State;

        Assert.That(result, Is.EqualTo(request));
        Assert.That(state.PendingRequests, Is.Empty);
        Assert.That(state.InFlightRequest, Is.EqualTo(request));
    }

    [Test]
    public void Dequeue_ThrowsException_WhenThereIsAlreadyAnInFlightItem()
    {
        var request1 = new TestRequest("test data 1");
        var request2 = new TestRequest("test data 2");
        _queue.Enqueue(request1);
        _queue.Enqueue(request2);
        _queue.Dequeue();

        var ex = Assert.Throws<InvalidOperationException>(() => _queue.Dequeue());
        Assert.That(ex.Message, Is.EqualTo("There is already a request in processing. Acknowledge or reject it first."));
    }

    [Test]
    public void Acknowledge_ClearsInFlightItem_WhenIdMatches()
    {
        var request = new TestRequest("test data");
        _queue.Enqueue(request);
        _queue.Dequeue();

        _queue.Acknowledge(request.Id);
        var state = _queue.State;

        Assert.That(state.InFlightRequest, Is.Null);
    }

    [Test]
    public void Acknowledge_ThrowsException_WhenIdDoesNotMatch()
    {
        var request = new TestRequest("test data");
        _queue.Enqueue(request);
        _queue.Dequeue();

        var ex = Assert.Throws<InvalidOperationException>(() => _queue.Acknowledge(Guid.NewGuid()));
        Assert.That(ex.Message, Is.EqualTo("Request in processing does not match the provided id to ack."));
    }

    [Test]
    public void Acknowledge_ThrowsException_WhenNoInFlightItem()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _queue.Acknowledge(Guid.NewGuid()));
        Assert.That(ex.Message, Is.EqualTo("Request in processing does not match the provided id to ack."));
    }

    [Test]
    public void Reject_ClearsInFlightItem_WhenIdMatches()
    {
        var request = new TestRequest("test data");
        _queue.Enqueue(request);
        _queue.Dequeue();

        _queue.Reject(request.Id, new Exception("Test exception"));
        var state = _queue.State;

        Assert.That(state.InFlightRequest, Is.Null);
    }

    [Test]
    public void Reject_ThrowsException_WhenIdDoesNotMatch()
    {
        var request = new TestRequest("test data");
        _queue.Enqueue(request);
        _queue.Dequeue();

        var ex = Assert.Throws<InvalidOperationException>(() => _queue.Reject(Guid.NewGuid(), new Exception("Test exception")));
        Assert.That(ex.Message, Is.EqualTo("Request in processing does not match the provided id to reject."));
    }

    [Test]
    public void Reject_ThrowsException_WhenNoInFlightItem()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _queue.Reject(Guid.NewGuid(), new Exception("Test exception")));
        Assert.That(ex.Message, Is.EqualTo("Request in processing does not match the provided id to reject."));
    }

    [Test]
    public void CompleteWorkflow_EnqueueDequeueAcknowledgeDequeue_WorksAsExpected()
    {
        var request1 = new TestRequest("test data 1");
        var request2 = new TestRequest("test data 2");

        _queue.Enqueue(request1);
        _queue.Enqueue(request2);

        var state1 = _queue.State;
        Assert.That(state1.PendingRequests, Has.Count.EqualTo(2));
        Assert.That(state1.InFlightRequest, Is.Null);

        var dequeued1 = _queue.Dequeue();
        Assert.That(dequeued1, Is.EqualTo(request1));

        var state2 = _queue.State;
        Assert.That(state2.PendingRequests, Has.Count.EqualTo(1));
        Assert.That(state2.InFlightRequest, Is.EqualTo(request1));

        _queue.Acknowledge(request1.Id);

        var state3 = _queue.State;
        Assert.That(state3.PendingRequests, Has.Count.EqualTo(1));
        Assert.That(state3.InFlightRequest, Is.Null);

        var dequeued2 = _queue.Dequeue();
        Assert.That(dequeued2, Is.EqualTo(request2));

        var state4 = _queue.State;
        Assert.That(state4.PendingRequests, Has.Count.EqualTo(0));
        Assert.That(state4.InFlightRequest, Is.EqualTo(request2));
    }

    [Test]
    public void CompleteWorkflow_EnqueueDequeueRejectDequeue_WorksAsExpected()
    {
        var request1 = new TestRequest("test data 1");
        var request2 = new TestRequest("test data 2");

        _queue.Enqueue(request1);
        _queue.Enqueue(request2);

        var dequeued1 = _queue.Dequeue();
        Assert.That(dequeued1, Is.EqualTo(request1));

        _queue.Reject(request1.Id, new Exception("Test exception"));

        var state = _queue.State;
        Assert.That(state.PendingRequests, Has.Count.EqualTo(1));
        Assert.That(state.InFlightRequest, Is.Null);

        var dequeued2 = _queue.Dequeue();
        Assert.That(dequeued2, Is.EqualTo(request2));
    }
}