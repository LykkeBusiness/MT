// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Hosting;

namespace MarginTrading.Backend.Services.Snapshot;

public sealed class SnapshotRequestsMonitor(
    ISnapshotBuilderService snapshotService,
    SnapshotMonitorSettings settings,
    ISnapshotRequestQueue queue) : BackgroundService
{
    private readonly ISnapshotBuilderService _snapshotService = snapshotService;
    private readonly ISnapshotRequestQueue _queue = queue;
    private readonly SnapshotMonitorSettings _settings = settings;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var request = _queue.Dequeue();
            if (request is null)
            {
                await Task.Delay(_settings.MonitoringDelay, stoppingToken);
                continue;
            }

            await _snapshotService.MakeTradingDataSnapshot(
                request.TradingDay,
                request.CorrelationId,
                request.ValidationStrategyType,
                request.Status);

            _queue.Acknowledge(request.Id);
        }
    }
}