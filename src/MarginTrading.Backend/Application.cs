﻿using System;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MarketMakerFeed;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;
using MarginTrading.OrderbookAggregator.Contracts.Messages;

#pragma warning disable 1591

namespace MarginTrading.Backend
{
    public sealed class Application
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IConsole _consoleWriter;
        private readonly MarketMakerService _marketMakerService;
        private readonly ILog _logger;
        private readonly MarginTradingSettings _marginSettings;
        private readonly IMaintenanceModeService _maintenanceModeService;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly MatchingEngineRoutesManager _matchingEngineRoutesManager;
        private readonly IMigrationService _migrationService;
        private readonly IConvertService _convertService;
        private readonly ExternalOrderBooksList _externalOrderBooksList;
        private const string ServiceName = "MarginTrading.Backend";

        public Application(
            IRabbitMqNotifyService rabbitMqNotifyService,
            IConsole consoleWriter,
            MarketMakerService marketMakerService,
            ILog logger, 
            MarginTradingSettings marginSettings,
            IMaintenanceModeService maintenanceModeService,
            IRabbitMqService rabbitMqService,
            MatchingEngineRoutesManager matchingEngineRoutesManager,
            IMigrationService migrationService,
            IConvertService convertService,
            ExternalOrderBooksList externalOrderBooksList)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _consoleWriter = consoleWriter;
            _marketMakerService = marketMakerService;
            _logger = logger;
            _marginSettings = marginSettings;
            _maintenanceModeService = maintenanceModeService;
            _rabbitMqService = rabbitMqService;
            _matchingEngineRoutesManager = matchingEngineRoutesManager;
            _migrationService = migrationService;
            _convertService = convertService;
            _externalOrderBooksList = externalOrderBooksList;
        }

        public async Task StartApplicationAsync()
        {
            _consoleWriter.WriteLine($"Starting {ServiceName}");
            await _logger.WriteInfoAsync(ServiceName, null, null, "Starting...");

            try
            {
                await _migrationService.InvokeAll();
                
                _rabbitMqService.Subscribe(
                    _marginSettings.MarketMakerRabbitMqSettings, false, HandleNewOrdersMessage,
                    _rabbitMqService.GetJsonDeserializer<MarketMakerOrderCommandsBatchMessage>());

                if (_marginSettings.RisksRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe(_marginSettings.RisksRabbitMqSettings,
                        true, _matchingEngineRoutesManager.HandleRiskManagerCommand,
                        _rabbitMqService.GetJsonDeserializer<MatchingEngineRouteRisksCommand>());
                }
                else if (_marginSettings.IsLive)
                {
                    _logger.WriteWarning(ServiceName, nameof(StartApplicationAsync),
                        "RisksRabbitMqSettings is not configured");
                }
                
                // Demo server works only in MM mode
                if (_marginSettings.IsLive)
                {
                    _rabbitMqService.Subscribe(_marginSettings.StpAggregatorRabbitMqSettings
                            .RequiredNotNull(nameof(_marginSettings.StpAggregatorRabbitMqSettings)), false, 
                        HandleStpOrderbook,
                        _rabbitMqService.GetMsgPackDeserializer<ExternalExchangeOrderbookMessage>());
                }
            }
            catch (Exception ex)
            {
                _consoleWriter.WriteLine($"{ServiceName} error: {ex.Message}");
                await _logger.WriteErrorAsync(ServiceName, "Application.RunAsync", null, ex);
            }
        }

        private Task HandleStpOrderbook(ExternalExchangeOrderbookMessage message)
        {
            var orderbook = _convertService.Convert<ExternalExchangeOrderbookMessage, ExternalOrderBook>(message);
            _externalOrderBooksList.SetOrderbook(orderbook);
            return Task.CompletedTask;
        }

        public void StopApplication()
        {
            _maintenanceModeService.SetMode(true);
            _consoleWriter.WriteLine($"Maintenance mode enabled for {ServiceName}");
            _consoleWriter.WriteLine($"Closing {ServiceName}");
            _logger.WriteInfoAsync(ServiceName, null, null, "Closing...").Wait();
            _rabbitMqNotifyService.Stop();
            _consoleWriter.WriteLine($"Closed {ServiceName}");
        }

        private Task HandleNewOrdersMessage(MarketMakerOrderCommandsBatchMessage feedData)
        {
            _marketMakerService.ProcessOrderCommands(feedData);
            return Task.CompletedTask;
        }
    }
}