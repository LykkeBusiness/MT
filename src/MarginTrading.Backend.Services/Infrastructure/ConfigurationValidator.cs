// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;

using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public sealed class ConfigurationValidator : IConfigurationValidator
    {
        private readonly ILogger<ConfigurationValidator> _logger;
        private readonly IFeatureManager _featureManager;
        private readonly MarginTradingSettings _settings;

        public ConfigurationValidator(
            ILogger<ConfigurationValidator> logger,
            IFeatureManager featureManager,
            MarginTradingSettings settings)
        {
            _logger = logger;
            _featureManager = featureManager;
            _settings = settings;
        }

        public async Task WarnIfInvalidAsync()
        {
            if (_settings.MarketMakerRabbitMqSettings == null &&
                _settings.StpAggregatorRabbitMqSettings == null)
            {
                throw new InvalidDataException("Both MM and STP connections are not configured. Can not start service.");
            }
            
            if (await _featureManager.IsEnabledAsync(Feature.CompiledSchedulePublishing.ToString("G")))
            {
                _logger.LogWarning("Compiled schedule publishing feature is obsolete but it is enabled.");
            }

            if (await _featureManager.IsEnabledAsync(Feature.TradeContractPublishing.ToString("G")))
            {
                _logger.LogWarning("Trade contract publishing feature is obsolete but it is enabled.");
            }

            if (_settings.MarketMakerRabbitMqSettings == null)
            {
                _logger.LogWarning("MarketMakerRabbitMqSettings is not configured");
            }

            if (_settings.StpAggregatorRabbitMqSettings == null)
            {
                _logger.LogWarning("StpAggregatorRabbitMqSettings is not configured");
            }

            if (_settings.RisksRabbitMqSettings == null)
            {
                _logger.LogWarning("RisksRabbitMqSettings is not configured");
            }
        }
    }
}