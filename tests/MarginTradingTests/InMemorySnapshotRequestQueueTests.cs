using MarginTrading.Backend.Services.Snapshots;

namespace MarginTradingTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Core.Snapshots;

using NUnit.Framework;

[TestFixture]
public class InMemorySnapshotRequestQueueTests
{
    [Test]
    public void SingleProducerSingleConsumer_EnqueueDequeueAcknowledge_ShouldProcessSuccessfully()
    {
        var queue = new InMemorySnapshotRequestQueue();
        var request = new SnapshotCreationRequest(
             Guid.NewGuid(),
            EnvironmentValidationStrategyType.AsSoonAsPossible,
            SnapshotStatus.Draft,
            DateTimeOffset.UtcNow,
            DateTime.UtcNow.Date
        );

        queue.Enqueue(request);
        var dequeued = queue.Dequeue();

        Assert.IsNotNull(dequeued);
        Assert.AreEqual(request, dequeued);

        queue.Acknowledge(request.Id);

        var afterAck = queue.Dequeue();
        Assert.IsNull(afterAck);
    }

    [Test]
    public void MultipleProducers_ConcurrentEnqueue_ShouldMaintainCorrectCountAndOrder()
    {
        var queue = new InMemorySnapshotRequestQueue();
        int producerCount = 10;
        int itemsPerProducer = 100;
        var tasks = new List<Task>();

        for (int p = 0; p < producerCount; p++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < itemsPerProducer; i++)
                {
                    var request = new SnapshotCreationRequest(
                        Guid.NewGuid(),
                        EnvironmentValidationStrategyType.WaitPlatformConsistency,
                        SnapshotStatus.Final,
                        DateTimeOffset.UtcNow,
                        DateTime.UtcNow.Date
                    );
                    queue.Enqueue(request);
                }
            }));
        }
        Task.WaitAll([.. tasks]);

        var state = queue.CaptureState();
        int expectedCount = producerCount * itemsPerProducer;
        Assert.AreEqual(expectedCount, state.PendingRequests.Count);

        var dequeuedItems = new List<SnapshotCreationRequest>();
        SnapshotCreationRequest current;
        while ((current = queue.Dequeue()) != null)
        {
            dequeuedItems.Add(current);
            queue.Acknowledge(current.Id);
        }
        Assert.AreEqual(expectedCount, dequeuedItems.Count);
    }

    [Test]
    public void ConsumerFailsToAcknowledge_ShouldRemainBlockedOnDequeue()
    {
        var queue = new InMemorySnapshotRequestQueue();
        var request1 = new SnapshotCreationRequest(
            Guid.NewGuid(),
            EnvironmentValidationStrategyType.AsSoonAsPossible,
            SnapshotStatus.Draft,
            DateTimeOffset.UtcNow,
            DateTime.UtcNow.Date
        );
        var request2 = new SnapshotCreationRequest(
            Guid.NewGuid(),
            EnvironmentValidationStrategyType.WaitPlatformConsistency,
            SnapshotStatus.Final,
            DateTimeOffset.UtcNow.AddSeconds(1),
            DateTime.UtcNow.Date
        );
        queue.Enqueue(request1);
        queue.Enqueue(request2);

        var firstDequeue = queue.Dequeue();
        Assert.IsNotNull(firstDequeue);
        Assert.AreEqual(request1, firstDequeue);

        // Dequeue should be blocked until the first item is acknowledged.
        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());

        queue.Acknowledge(request1.Id);

        var thirdDequeue = queue.Dequeue();
        Assert.IsNotNull(thirdDequeue);
        Assert.AreEqual(request2, thirdDequeue);
    }

    [Test]
    public void MismatchedAcknowledge_ShouldNotClearInFlightItem()
    {
        var queue = new InMemorySnapshotRequestQueue();
        var request = new SnapshotCreationRequest(
            Guid.NewGuid(),
            EnvironmentValidationStrategyType.AsSoonAsPossible,
            SnapshotStatus.Draft,
            DateTimeOffset.UtcNow,
            DateTime.UtcNow.Date
        );
        queue.Enqueue(request);
        var dequeued = queue.Dequeue();

        Assert.Throws<InvalidOperationException>(() => queue.Acknowledge(Guid.NewGuid()));

        // Dequeue should still throw since the current request is not acknowledged yet.
        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());

        // Now acknowledge correctly.
        queue.Acknowledge(request.Id);
    }

    [Test]
    public void GracefulShutdown_CaptureAndRestoreState_ShouldMaintainQueueState()
    {
        var queue = new InMemorySnapshotRequestQueue();
        var request1 = new SnapshotCreationRequest(
            Guid.NewGuid(),
            EnvironmentValidationStrategyType.AsSoonAsPossible,
            SnapshotStatus.Draft,
            DateTimeOffset.UtcNow,
            DateTime.UtcNow.Date
        );
        var request2 = new SnapshotCreationRequest(
            Guid.NewGuid(),
            EnvironmentValidationStrategyType.WaitPlatformConsistency,
            SnapshotStatus.Final,
            DateTimeOffset.UtcNow.AddSeconds(1),
            DateTime.UtcNow.Date
        );

        queue.Enqueue(request1);
        queue.Enqueue(request2);

        // Dequeue to get the first request in-flight.
        var inFlight = queue.Dequeue();
        Assert.IsNotNull(inFlight);
        Assert.AreEqual(request1, inFlight);

        // Capture the current state (simulate shutdown).
        var state = queue.CaptureState();

        // Create a new queue instance and restore the state.
        var restoredQueue = new InMemorySnapshotRequestQueue();
        restoredQueue.RestoreState(state);

        // Assert that the in-flight item is restored.
        var restoredInFlight = restoredQueue.Dequeue();
        Assert.IsNotNull(restoredInFlight);
        Assert.AreEqual(request1, restoredInFlight);

        // Acknowledge it then get the next item.
        restoredQueue.Acknowledge(restoredInFlight.Id);
        var nextItem = restoredQueue.Dequeue();
        Assert.IsNotNull(nextItem);
        Assert.AreEqual(request2, nextItem);
    }

    [Test]
    public void RestoreQueueState_When_It_Is_Not_Empty_Should_Throw()
    {
        var queue = new InMemorySnapshotRequestQueue();
        var request = new SnapshotCreationRequest(
            Guid.NewGuid(),
            EnvironmentValidationStrategyType.AsSoonAsPossible,
            SnapshotStatus.Draft,
            DateTimeOffset.UtcNow,
            DateTime.UtcNow.Date
        );
        queue.Enqueue(request);

        var state = queue.CaptureState();
        Assert.Throws<InvalidOperationException>(() => queue.RestoreState(state));
    }

    [Test]
    public void FifoOrder_ShouldMaintainOrderOfRequests()
    {
        // Arrange
        var queue = new InMemorySnapshotRequestQueue();
        int itemCount = 5;
        var requests = new List<SnapshotCreationRequest>();

        // Enqueue several items with increasing timestamps.
        for (int i = 0; i < itemCount; i++)
        {
            var request = new SnapshotCreationRequest(
                Guid.NewGuid(),
                EnvironmentValidationStrategyType.AsSoonAsPossible,
                SnapshotStatus.Draft,
                DateTimeOffset.UtcNow.AddSeconds(i),
                DateTime.UtcNow.Date
            );
            requests.Add(request);
            queue.Enqueue(request);
        }

        // Act & Assert: Dequeue (and acknowledge) all items in FIFO order.
        foreach (var expected in requests)
        {
            var dequeued = queue.Dequeue();
            Assert.IsNotNull(dequeued);
            Assert.AreEqual(expected, dequeued);
            queue.Acknowledge(dequeued.Id);
        }

        // Final dequeue should be null since the queue is empty.
        var finalDequeue = queue.Dequeue();
        Assert.IsNull(finalDequeue);
    }
}
