// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Snapshots;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services
{
    public class SnapshotMonitorService : BackgroundService
    {
        private readonly ISnapshotMonitor _snapshotMonitor;
        private readonly ISnapshotService _snapshotService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly SnapshotMonitorSettings _settings;
        private readonly ILogger<SnapshotMonitorService> _logger;

        public SnapshotMonitorService(
            ISnapshotMonitor snapshotMonitor,
            ISnapshotService snapshotService,
            IIdentityGenerator identityGenerator,
            SnapshotMonitorSettings settings,
            ILogger<SnapshotMonitorService> logger)
        {
            _snapshotMonitor = snapshotMonitor;
            _snapshotService = snapshotService;
            _identityGenerator = identityGenerator;
            _settings = settings;
            _logger = logger;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{ServiceName} started", nameof(SnapshotMonitorService));
            
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_snapshotMonitor.ShouldRetrySnapshot(out var tradingDay))
                {
                    _logger.LogWarning("{ServiceName}: Trading Snapshot Draft was requested, but timeout exceeded. Attempting to create the snapshot.",
                        nameof(SnapshotMonitorService));
                    
                    try
                    {
                        await _snapshotService.MakeTradingDataSnapshot(tradingDay,
                            _identityGenerator.GenerateGuid(),
                            SnapshotStatus.Draft);
                        
                        _logger.LogInformation("{ServiceName}: Trading Snapshot Draft was created",
                            nameof(SnapshotMonitorService));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex,
                            "Could not create trading data snapshot for {TradingDay}. {Message}",
                            tradingDay,
                            ex.Message);
                        
                        // exception is swallowed to allow retries
                    }
                }
                
                await Task.Delay(_settings.MonitoringDelay, stoppingToken);
            }
        }
    }
}