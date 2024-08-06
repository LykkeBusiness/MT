// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using Common.Log;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.AssetService.Contracts.Messages;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MarketMakerFeed;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;

namespace MarginTrading.Backend
{
    public sealed class Application
    {
        private readonly ILog _logger;
        private readonly IMaintenanceModeService _maintenanceModeService;
        private readonly IMigrationService _migrationService;
        private readonly RabbitMqListener<BrokerSettingsChangedEvent> _brokerSettingsChangedListener;
        private readonly RabbitMqListener<MarketMakerOrderCommandsBatchMessage> _marketMakerOrderCommandsBatchListener;

        private readonly RabbitMqListener<FxRateExternalExchangeOrderbookMessage>
            _fxRateExternalExchangeOrderbookListener;

        private readonly RabbitMqListener<StpAggregatorExternalExchangeOrderbookMessage>
            _stpAggregatorExternalExchangeOrderbookListener;

        private readonly RabbitMqListener<MatchingEngineRouteRisksCommand> _matchingEngineRouteRisksCommandListener;
        private readonly RabbitMqListener<SettingsChangedEvent> _settingsChangedListener;
        private const string ServiceName = "MarginTrading.Backend";

        public Application(
            ILog logger,
            IMaintenanceModeService maintenanceModeService,
            IMigrationService migrationService,
            RabbitMqListener<BrokerSettingsChangedEvent> brokerSettingsChangedListener,
            RabbitMqListener<SettingsChangedEvent> settingsChangedListener,
            RabbitMqListener<MarketMakerOrderCommandsBatchMessage> marketMakerOrderCommandsBatchListener = null,
            RabbitMqListener<FxRateExternalExchangeOrderbookMessage> fxRateExternalExchangeOrderbookListener = null,
            RabbitMqListener<StpAggregatorExternalExchangeOrderbookMessage>
                stpAggregatorExternalExchangeOrderbookListener = null,
            RabbitMqListener<MatchingEngineRouteRisksCommand> matchingEngineRouteRisksCommandListener = null)
        {
            _logger = logger;
            _maintenanceModeService = maintenanceModeService;
            _migrationService = migrationService;
            _brokerSettingsChangedListener = brokerSettingsChangedListener;
            _settingsChangedListener = settingsChangedListener;
            _marketMakerOrderCommandsBatchListener = marketMakerOrderCommandsBatchListener;
            _fxRateExternalExchangeOrderbookListener = fxRateExternalExchangeOrderbookListener;
            _stpAggregatorExternalExchangeOrderbookListener = stpAggregatorExternalExchangeOrderbookListener;
            _matchingEngineRouteRisksCommandListener = matchingEngineRouteRisksCommandListener;
        }

        public async Task StartApplicationAsync()
        {
            await _logger.WriteInfoAsync(nameof(StartApplicationAsync), nameof(Application), $"Starting {ServiceName}");

            try
            {
                await _migrationService.InvokeAll();

                StartListenerWithLogging(_marketMakerOrderCommandsBatchListener);
                StartListenerWithLogging(_fxRateExternalExchangeOrderbookListener);
                StartListenerWithLogging(_stpAggregatorExternalExchangeOrderbookListener);
                StartListenerWithLogging(_matchingEngineRouteRisksCommandListener);
                StartListenerWithLogging(_settingsChangedListener);
                StartListenerWithLogging(_brokerSettingsChangedListener);
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync(
                    ServiceName,
                    "Application.RunAsync",
                    null,
                    ex);
            }
        }

        public void StopApplication()
        {
            _maintenanceModeService.SetMode(true);
            _logger.WriteInfoAsync(
                ServiceName,
                null,
                null,
                "Application is shutting down").Wait();
        }

        private void StartListenerWithLogging<T>([CanBeNull] RabbitMqListener<T> listener) where T : class
        {
            if (listener == null)
                return;

            try
            {
                listener.Start();
                _logger.WriteInfo(
                    nameof(StartListenerWithLogging),
                    nameof(Application),
                    $"Listener for {typeof(T).Name} successfully started");
            }
            catch (Exception ex)
            {
                _logger.WriteError(nameof(StartListenerWithLogging), nameof(Application), ex);
                throw;
            }
        }
    }
}