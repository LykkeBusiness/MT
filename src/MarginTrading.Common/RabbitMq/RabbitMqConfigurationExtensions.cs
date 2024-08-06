// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Lykke.RabbitMqBroker;

namespace MarginTrading.Common.RabbitMq
{
    public static class RabbitMqConfigurationExtensions
    {
        public static RabbitMqPublisherConfigurationWithLogging ToPublisherConfigWithLogging(
            this RabbitMqConfigurationBase configuration, 
            bool logEventPublishing)
        {
            return new RabbitMqPublisherConfigurationWithLogging
            {
                ExchangeName = configuration.ExchangeName,
                ConnectionString = configuration.ConnectionString,
                IsDurable = configuration.IsDurable,
                LogEventPublishing = logEventPublishing,
            };
        }

        public static RabbitMqSubscriptionSettings ToInstanceSubscriptionSettings(
            this RabbitMqConsumerConfiguration config,
            string instanceId,
            bool isDurable)
        {
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = config.ConnectionString,
                QueueName = QueueHelper.BuildQueueName(config.ExchangeName, env: instanceId),
                ExchangeName = config.ExchangeName,
                IsDurable = isDurable,
                RoutingKey = config.RoutingKey
            };
        }

        public static void SetConnectionString(this IEnumerable<RabbitMqConfigurationBase> src, string connectionString)
        {
            if (src == null)
                return;

            foreach (var config in src)
            {
                config.ConnectionString = connectionString;
            }
        }
    }
}