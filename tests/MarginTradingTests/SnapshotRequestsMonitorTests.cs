using System;
using System.Threading;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.Snapshots;

using Moq;

using NUnit.Framework;

namespace MarginTradingTests;

[TestFixture]
public class SnapshotRequestsMonitorTests
{
    /// <summary>
    /// Tests that when the queue always returns null (i.e. no requests),
    /// the snapshot service is never invoked.
    /// </summary>
    [Test]
    public async Task ExecuteAsync_WhenQueueReturnsNull_ShouldNotCallSnapshotService()
    {
        var snapshotServiceMock = new Mock<ISnapshotBuilderService>();
        var queueMock = new Mock<IWaitableRequestConsumer<SnapshotCreationRequest, TradingEngineSnapshotSummary>>();
        queueMock.Setup(q => q.Dequeue()).Returns((SnapshotCreationRequest)null);

        var settings = new SnapshotMonitorSettings
        {
            MonitoringDelay = TimeSpan.FromMilliseconds(10)
        };

        var sut = new SnapshotRequestsMonitor(
            snapshotServiceMock.Object,
            settings,
            queueMock.Object);

        using var cts = new CancellationTokenSource(50);

        await sut.StartAsync(cts.Token);
        await Task.Delay(30, cts.Token);
        await sut.StopAsync(cts.Token);

        snapshotServiceMock.Verify(
            s => s.MakeSnapshot(
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<EnvironmentValidationStrategyType>(),
                It.IsAny<SnapshotInitiator>(),
                It.IsAny<SnapshotStatus>()),
            Times.Never);
    }

    /// <summary>
    /// Tests that when the queue returns a valid request, the monitor calls
    /// the snapshot service with the correct parameters and then acknowledges the request.
    /// </summary>
    [Test]
    public async Task ExecuteAsync_WhenQueueReturnsValidRequest_ShouldCallSnapshotServiceAndAcknowledge()
    {
        var validRequest = new SnapshotCreationRequest(
            Guid.NewGuid(),
            EnvironmentValidationStrategyType.AsSoonAsPossible,
            SnapshotStatus.Draft,
            SnapshotInitiator.ServiceApi,
            DateTimeOffset.UtcNow,
            new DateTime(2025, 2, 12),
            Guid.NewGuid().ToString()
        );

        var snapshotServiceMock = new Mock<ISnapshotBuilderService>();
        snapshotServiceMock
            .Setup(s => s.MakeSnapshot(
            validRequest.TradingDay,
            validRequest.CorrelationId,
            validRequest.ValidationStrategyType,
            validRequest.Initiator,
            validRequest.Status))
            .Returns(Task.FromResult(TradingEngineSnapshotSummary.Empty))
            .Verifiable();

        var callCount = 0;
        var queueMock = new Mock<IWaitableRequestQueue<SnapshotCreationRequest, TradingEngineSnapshotSummary>>();
        queueMock.Setup(q => q.Dequeue()).Returns(() =>
        {
            if (callCount == 0)
            {
                callCount++;
                return validRequest;
            }
            else
            {
                return null;
            }
        });
        queueMock.Setup(q => q.Acknowledge(validRequest.Id, It.IsAny<TradingEngineSnapshotSummary>())).Verifiable();

        var settings = new SnapshotMonitorSettings
        {
            MonitoringDelay = TimeSpan.FromMilliseconds(10)
        };

        var sut = new SnapshotRequestsMonitor(
            snapshotServiceMock.Object,
            settings,
            queueMock.Object);

        using var cts = new CancellationTokenSource(100);

        await sut.StartAsync(cts.Token);
        await Task.Delay(50, cts.Token);
        await sut.StopAsync(cts.Token);

        snapshotServiceMock.Verify(
            s => s.MakeSnapshot(
            validRequest.TradingDay,
            validRequest.CorrelationId,
            validRequest.ValidationStrategyType,
            validRequest.Initiator,
            validRequest.Status),
            Times.Once);
        queueMock.Verify(q => q.Acknowledge(validRequest.Id, It.IsAny<TradingEngineSnapshotSummary>()), Times.Once);
    }
}