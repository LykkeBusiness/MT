﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Snow.Mdm.Contracts.Models.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MarketMakerFeed;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;
using MarginTrading.OrderbookAggregator.Contracts.Messages;
using MarginTrading.AssetService.Contracts.Enums;
using MarginTrading.AssetService.Contracts.Messages;
using MarginTrading.Backend.Services.Services;

#pragma warning disable 1591

namespace MarginTrading.Backend
{
    public sealed class Application
    {
        private readonly MarketMakerService _marketMakerService;
        private readonly ILog _logger;
        private readonly MarginTradingSettings _marginSettings;
        private readonly IMaintenanceModeService _maintenanceModeService;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly IMatchingEngineRoutesManager _matchingEngineRoutesManager;
        private readonly IMigrationService _migrationService;
        private readonly IConvertService _convertService;
        private readonly IFxRateCacheService _fxRateCacheService;
        private readonly IExternalOrderbookService _externalOrderbookService;
        private readonly BrokerSettingsChangedHandler _brokerSettingsChangedHandler;
        private const string ServiceName = "MarginTrading.Backend";

        public Application(
            MarketMakerService marketMakerService,
            ILog logger,
            MarginTradingSettings marginSettings,
            IMaintenanceModeService maintenanceModeService,
            IRabbitMqService rabbitMqService,
            MatchingEngineRoutesManager matchingEngineRoutesManager,
            IMigrationService migrationService,
            IConvertService convertService,
            IFxRateCacheService fxRateCacheService,
            IExternalOrderbookService externalOrderbookService,
            BrokerSettingsChangedHandler brokerSettingsChangedHandler)
        {
            _marketMakerService = marketMakerService;
            _logger = logger;
            _marginSettings = marginSettings;
            _maintenanceModeService = maintenanceModeService;
            _rabbitMqService = rabbitMqService;
            _matchingEngineRoutesManager = matchingEngineRoutesManager;
            _migrationService = migrationService;
            _convertService = convertService;
            _fxRateCacheService = fxRateCacheService;
            _externalOrderbookService = externalOrderbookService;
            _brokerSettingsChangedHandler = brokerSettingsChangedHandler;
        }

        public async Task StartApplicationAsync()
        {
            await _logger.WriteInfoAsync(nameof(StartApplicationAsync), nameof(Application), $"Starting {ServiceName}");

            if (_marginSettings.MarketMakerRabbitMqSettings == null &&
                _marginSettings.StpAggregatorRabbitMqSettings == null)
            {
                throw new Exception("Both MM and STP connections are not configured. Can not start service.");
            }

            try
            {
                await _migrationService.InvokeAll();

                if (_marginSettings.MarketMakerRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe(
                        _marginSettings.MarketMakerRabbitMqSettings, false, HandleNewOrdersMessage,
                        _rabbitMqService.GetJsonDeserializer<MarketMakerOrderCommandsBatchMessage>());
                }
                else
                {
                    _logger.WriteInfo(ServiceName, nameof(StartApplicationAsync),
                        "MarketMakerRabbitMqSettings is not configured");
                }

                if (_marginSettings.FxRateRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe(_marginSettings.FxRateRabbitMqSettings, false,
                        _fxRateCacheService.SetQuote, _rabbitMqService.GetMsgPackDeserializer<ExternalExchangeOrderbookMessage>());
                }

                if (_marginSettings.StpAggregatorRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe(_marginSettings.StpAggregatorRabbitMqSettings,
                        false, HandleStpOrderbook,
                        _rabbitMqService.GetMsgPackDeserializer<ExternalExchangeOrderbookMessage>());
                }
                else
                {
                    _logger.WriteInfo(ServiceName, nameof(StartApplicationAsync),
                        "StpAggregatorRabbitMqSettings is not configured");
                }

                if (_marginSettings.RisksRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe(_marginSettings.RisksRabbitMqSettings,
                        true, _matchingEngineRoutesManager.HandleRiskManagerCommand,
                        _rabbitMqService.GetJsonDeserializer<MatchingEngineRouteRisksCommand>());
                }
                else
                {
                    _logger.WriteInfo(ServiceName, nameof(StartApplicationAsync),
                        "RisksRabbitMqSettings is not configured");
                }

                _rabbitMqService.Subscribe(_marginSettings.SettingsChangedRabbitMqSettings,
                    true, HandleChangeSettingsMessage,
                    _rabbitMqService.GetJsonDeserializer<SettingsChangedEvent>());

                _rabbitMqService.Subscribe(_marginSettings.BrokerSettingsRabbitMqSettings, false,
                    _brokerSettingsChangedHandler.Handle,
                    _rabbitMqService.GetMsgPackDeserializer<BrokerSettingsChangedEvent>());
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync(ServiceName, "Application.RunAsync", null, ex);
            }
        }

        private Task HandleStpOrderbook(ExternalExchangeOrderbookMessage message)
        {
            var orderbook = _convertService.Convert<ExternalExchangeOrderbookMessage, ExternalOrderBook>(message);
            _externalOrderbookService.SetOrderbook(orderbook);
            return Task.CompletedTask;
        }

        public void StopApplication()
        {
            _maintenanceModeService.SetMode(true);
            _logger.WriteInfoAsync(ServiceName, null, null, "Application is shutting down").Wait();
        }

        private Task HandleNewOrdersMessage(MarketMakerOrderCommandsBatchMessage feedData)
        {
            _marketMakerService.ProcessOrderCommands(feedData);
            return Task.CompletedTask;
        }

        private async Task HandleChangeSettingsMessage(SettingsChangedEvent message)
        {
            if (message.SettingsType == SettingsTypeContract.TradingRoute)
                await _matchingEngineRoutesManager.UpdateRoutesCacheAsync();
        }
    }
}