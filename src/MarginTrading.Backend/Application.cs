// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common.Log;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Infrastructure;

namespace MarginTrading.Backend
{
    public sealed class Application
    {
        private readonly ILog _logger;
        private readonly IMaintenanceModeService _maintenanceModeService;
        private readonly IMigrationService _migrationService;
        private const string ServiceName = "MarginTrading.Backend";

        public Application(
            ILog logger,
            IMaintenanceModeService maintenanceModeService,
            IMigrationService migrationService)
        {
            _logger = logger;
            _maintenanceModeService = maintenanceModeService;
            _migrationService = migrationService;
        }

        public async Task StartApplicationAsync()
        {
            await _logger.WriteInfoAsync(nameof(StartApplicationAsync), nameof(Application), $"Starting {ServiceName}");

            try
            {
                await _migrationService.InvokeAll();
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync(ServiceName, "Application.RunAsync", null, ex);
            }
        }

        public void StopApplication()
        {
            _maintenanceModeService.SetMode(true);
            _logger.WriteInfoAsync(ServiceName, null, null, "Application is shutting down").Wait();
        }
    }
}