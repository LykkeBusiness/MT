// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Common.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Snapshot
{
    /// <summary>
    /// Attempts to build a draft snapshot when the platform is degraded (specifically, when there's an issue with rabbitmq)
    /// </summary>
    public class DraftSnapshotMonitor : BackgroundService
    {
        private readonly IDraftSnapshotWorkflowTracker _draftSnapshotWorkflowTracker;
        private readonly ISnapshotBuilderService _snapshotService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly SnapshotMonitorSettings _settings;
        private readonly IDateService _dateService;
        private readonly ILogger<DraftSnapshotMonitor> _logger;

        public DraftSnapshotMonitor(
            IDraftSnapshotWorkflowTracker draftSnapshotWorkflowTracker,
            ISnapshotBuilderService snapshotService,
            IIdentityGenerator identityGenerator,
            SnapshotMonitorSettings settings,
            ILogger<DraftSnapshotMonitor> logger,
            IDateService dateService)
        {
            _draftSnapshotWorkflowTracker = draftSnapshotWorkflowTracker;
            _snapshotService = snapshotService;
            _identityGenerator = identityGenerator;
            _settings = settings;
            _logger = logger;
            _dateService = dateService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{ServiceName} started", nameof(DraftSnapshotMonitor));

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_draftSnapshotWorkflowTracker.Current is not DraftSnapshotWorkflowState.Requested)
                {
                    await MonitoringDelay(stoppingToken);
                    continue;
                }

                if (!_draftSnapshotWorkflowTracker.IsTimePassed(
                    _settings.DelayBeforeFallbackSnapshot,
                    _dateService.Now()))
                {
                    await MonitoringDelay(stoppingToken);
                }

                _logger.LogWarning("{ServiceName}: Trading Snapshot Draft was requested, but timeout exceeded. Attempting to create the snapshot.",
                    nameof(DraftSnapshotMonitor));

                try
                {
                    await _snapshotService.MakeTradingDataSnapshot(_draftSnapshotWorkflowTracker.TradingDay,
                        _identityGenerator.GenerateGuid(),
                        SnapshotStatus.Draft);

                    _logger.LogInformation("{ServiceName}: Trading Snapshot Draft was created",
                        nameof(DraftSnapshotMonitor));
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex,
                        "Could not create trading data snapshot for {TradingDay}. {Message}",
                        _draftSnapshotWorkflowTracker.TradingDay,
                        ex.Message);
                }
            }
        }

        private Task MonitoringDelay(CancellationToken stoppingToken) =>
            Task.Delay(_settings.MonitoringDelay, stoppingToken);
    }
}