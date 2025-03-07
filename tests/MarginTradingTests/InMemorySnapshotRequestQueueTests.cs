namespace MarginTradingTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services;

using NUnit.Framework;

[TestFixture]
public class InMemorySnapshotRequestQueueTests
{
    [Test]
    public void SingleProducerSingleConsumer_EnqueueDequeueAcknowledge_ShouldProcessSuccessfully()
    {
        var queue = new InMemoryRequestQueue<SnapshotCreationRequest>();
        var request = new SnapshotCreationRequest(
             Guid.NewGuid(),
            EnvironmentValidationStrategyType.AsSoonAsPossible,
            SnapshotStatus.Draft,
            SnapshotInitiator.ServiceApi,
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
        var queue = new InMemoryRequestQueue<SnapshotCreationRequest>();
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
                        SnapshotInitiator.ServiceApi,
                        DateTimeOffset.UtcNow,
                        DateTime.UtcNow.Date
                    );
                    queue.Enqueue(request);
                }
            }));
        }
        Task.WaitAll([.. tasks]);

        int expectedCount = producerCount * itemsPerProducer;
        Assert.AreEqual(expectedCount, queue.State.PendingRequests.Count);

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
        var queue = new InMemoryRequestQueue<SnapshotCreationRequest>();
        var request1 = new SnapshotCreationRequest(
            Guid.NewGuid(),
            EnvironmentValidationStrategyType.AsSoonAsPossible,
            SnapshotStatus.Draft,
            SnapshotInitiator.ServiceApi,
            DateTimeOffset.UtcNow,
            DateTime.UtcNow.Date
        );
        var request2 = new SnapshotCreationRequest(
            Guid.NewGuid(),
            EnvironmentValidationStrategyType.WaitPlatformConsistency,
            SnapshotStatus.Final,
            SnapshotInitiator.ServiceApi,
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
        var queue = new InMemoryRequestQueue<SnapshotCreationRequest>();
        var request = new SnapshotCreationRequest(
            Guid.NewGuid(),
            EnvironmentValidationStrategyType.AsSoonAsPossible,
            SnapshotStatus.Draft,
            SnapshotInitiator.ServiceApi,
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
    public void FifoOrder_ShouldMaintainOrderOfRequests()
    {
        // Arrange
        var queue = new InMemoryRequestQueue<SnapshotCreationRequest>();
        int itemCount = 5;
        var requests = new List<SnapshotCreationRequest>();

        // Enqueue several items with increasing timestamps.
        for (int i = 0; i < itemCount; i++)
        {
            var request = new SnapshotCreationRequest(
                Guid.NewGuid(),
                EnvironmentValidationStrategyType.AsSoonAsPossible,
                SnapshotStatus.Draft,
                SnapshotInitiator.ServiceApi,
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
