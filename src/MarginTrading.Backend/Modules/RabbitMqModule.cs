// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;

using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Snow.Common.Correlation.RabbitMq;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.AssetService.Contracts.Messages;
using MarginTrading.Backend.Core.MarketMakerFeed;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.MessageHandlers;
using MarginTrading.Common.RabbitMq;
using MarginTrading.OrderbookAggregator.Contracts.Messages;

namespace MarginTrading.Backend.Modules
{
    public class RabbitMqModule : Module
    {
        private readonly MarginTradingSettings _settings;

        public RabbitMqModule(MarginTradingSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.AddRabbitMqConnectionProvider();

            if (_settings.MarketMakerRabbitMqSettings != null)
            {
                builder.AddRabbitMqListener<MarketMakerOrderCommandsBatchMessage, NewOrdersHandler>(
                        _settings.MarketMakerRabbitMqSettings.ToInstanceSubscriptionSettings(_settings.Env, false),
                        ConfigureCorrelationManager)
                    .AddOptions(
                        opt =>
                        {
                            opt.SerializationFormat = SerializationFormat.Json;
                            opt.ShareConnection = true;
                            opt.SubscriptionTemplate = SubscriptionTemplate.NoLoss;
                            opt.ConsumerCount = (byte)_settings.MarketMakerRabbitMqSettings.ConsumerCount;
                        })
                    .AutoStart();
            }

            if (_settings.FxRateRabbitMqSettings != null)
            {
                builder.AddRabbitMqListener<ExternalExchangeOrderbookMessage, FxRateExternalExchangeOrderbookHandler>(
                        _settings.FxRateRabbitMqSettings.ToInstanceSubscriptionSettings(_settings.Env, false),
                        ConfigureCorrelationManager)
                    .AddOptions(
                        opt =>
                        {
                            opt.SerializationFormat = SerializationFormat.Messagepack;
                            opt.ShareConnection = true;
                            opt.SubscriptionTemplate = SubscriptionTemplate.LossAcceptable;
                            opt.ConsumerCount = (byte)_settings.FxRateRabbitMqSettings.ConsumerCount;
                        })
                    .AutoStart();
            }

            if (_settings.StpAggregatorRabbitMqSettings != null)
            {
                builder.AddRabbitMqListener<ExternalExchangeOrderbookMessage, ExternalExchangeOrderbookMessageHandler>(
                        _settings.StpAggregatorRabbitMqSettings.ToInstanceSubscriptionSettings(_settings.Env, false),
                        ConfigureCorrelationManager)
                    .AddOptions(
                        opt =>
                        {
                            opt.SerializationFormat = SerializationFormat.Messagepack;
                            opt.ShareConnection = true;
                            opt.SubscriptionTemplate = SubscriptionTemplate.LossAcceptable;
                            opt.ConsumerCount = (byte)_settings.StpAggregatorRabbitMqSettings.ConsumerCount;
                        })
                    .AutoStart();
            }

            if (_settings.RisksRabbitMqSettings != null)
            {
                builder.AddRabbitMqListener<MatchingEngineRouteRisksCommand, RiskManagerCommandHandler>(
                        _settings.RisksRabbitMqSettings.ToInstanceSubscriptionSettings(_settings.Env, true),
                        ConfigureCorrelationManager)
                    .AddOptions(
                        opt =>
                        {
                            opt.SerializationFormat = SerializationFormat.Json;
                            opt.ShareConnection = true;
                            opt.SubscriptionTemplate = SubscriptionTemplate.NoLoss;
                            opt.ConsumerCount = (byte)_settings.RisksRabbitMqSettings.ConsumerCount;
                        })
                    .AutoStart();
            }

            builder.AddRabbitMqListener<SettingsChangedEvent, SettingsChangedHandler>(
                    _settings.SettingsChangedRabbitMqSettings.ToInstanceSubscriptionSettings(_settings.Env, true),
                    ConfigureCorrelationManager)
                .AddOptions(
                    opt =>
                    {
                        opt.SerializationFormat = SerializationFormat.Json;
                        opt.ShareConnection = true;
                        opt.SubscriptionTemplate = SubscriptionTemplate.NoLoss;
                        opt.ConsumerCount = (byte)_settings.SettingsChangedRabbitMqSettings.ConsumerCount;
                    })
                .AutoStart();

            builder.AddRabbitMqListener<BrokerSettingsChangedEvent, BrokerSettingsChangedHandler>(
                    _settings.BrokerSettingsRabbitMqSettings.ToInstanceSubscriptionSettings(_settings.Env, false),
                    ConfigureCorrelationManager)
                .AddOptions(
                    opt =>
                    {
                        opt.SerializationFormat = SerializationFormat.Messagepack;
                        opt.ShareConnection = true;
                        opt.SubscriptionTemplate = SubscriptionTemplate.NoLoss;
                        opt.ConsumerCount = (byte)_settings.BrokerSettingsRabbitMqSettings.ConsumerCount;
                    })
                .AutoStart();
        }

        private static void ConfigureCorrelationManager<T>(RabbitMqSubscriber<T> subscriber, IComponentContext ctx)
        {
            var correlationManager = ctx.Resolve<RabbitMqCorrelationManager>();
            subscriber.SetReadHeadersAction(correlationManager.FetchCorrelationIfExists);
        }
    }
}