// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Hosting;

namespace MarginTrading.Backend.Services.Snapshots;

public sealed class SnapshotRequestsMonitor(
    ISnapshotBuilderService snapshotService,
    SnapshotMonitorSettings settings,
    IWaitableRequestConsumer<SnapshotCreationRequest, TradingEngineSnapshotSummary> queue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(settings.MonitoringDelay);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var request = queue.Dequeue();
            if (request is null)
                continue;

            TradingEngineSnapshotSummary result;
            try
            {
                result = await snapshotService.MakeSnapshot(request);
            }
            catch (Exception e)
            {
                queue.Reject(request.Id, e);
                continue;
            }
            queue.Acknowledge(request.Id, result);
        }
    }
}