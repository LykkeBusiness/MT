// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Hosting;

namespace MarginTrading.Backend.Services.Snapshots;

public sealed class SnapshotRequestsMonitor(
    ISnapshotBuilderService snapshotService,
    SnapshotMonitorSettings settings,
    ISnapshotRequestQueue queue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(settings.MonitoringDelay);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var request = queue.Dequeue();
            if (request is null)
                continue;

            await snapshotService.MakeSnapshot(
                request.TradingDay,
                request.CorrelationId,
                request.ValidationStrategyType,
                request.Status);

            queue.Acknowledge(request.Id);
        }
    }
}