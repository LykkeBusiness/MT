// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.Snow.Common.Correlation.RabbitMq;

using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IPublishingQueueRepository _publishingQueueRepository;
        private readonly RabbitMqCorrelationManager _correlationManager;

        private readonly ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStartStop>> _producers =
            new ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStartStop>>(
                new SubscriptionSettingsEqualityComparer());

        private const short QueueNotFoundErrorCode = 404;

        public RabbitMqService(
            ILoggerFactory loggerFactory,
            IPublishingQueueRepository publishingQueueRepository,
            RabbitMqCorrelationManager correlationManager)
        {
            _loggerFactory = loggerFactory;
            _publishingQueueRepository = publishingQueueRepository;
            _correlationManager = correlationManager;
        }

        /// <summary>
        /// Returns the number of messages in <paramref name="queueName"/> ready to be delivered to consumers.
        /// This method assumes the queue exists. If it doesn't, an exception is thrown.
        /// </summary>
        public static uint GetMessageCount(string connectionString, string queueName)
        {
            var factory = new ConnectionFactory { Uri = new Uri(connectionString, UriKind.Absolute) };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            try
            {
                return channel.QueueDeclarePassive(queueName).MessageCount;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == QueueNotFoundErrorCode)
            {
                return 0;
            }
        }

        public void Dispose()
        {
            foreach (var stoppable in _producers.Values)
                stoppable.Value.Stop();
        }

        public IRabbitMqSerializer<TMessage> GetJsonSerializer<TMessage>()
        {
            return new JsonMessageSerializer<TMessage>();
        }

        public IMessageProducer<TMessage> GetProducer<TMessage>(RabbitMqPublisherConfiguration configuration,
            IRabbitMqSerializer<TMessage> serializer)
        {
            // on-the fly connection strings switch is not supported currently for rabbitMq
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = configuration.ConnectionString,
                ExchangeName = configuration.ExchangeName,
                IsDurable = configuration.IsDurable,
            };

            return (IMessageProducer<TMessage>)_producers.GetOrAdd(subscriptionSettings, CreateProducer).Value;

            Lazy<IStartStop> CreateProducer(RabbitMqSubscriptionSettings s)
            {
                // Lazy ensures RabbitMqPublisher will be created and started only once
                // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
                return new Lazy<IStartStop>(() =>
                {
                    var publisher = new RabbitMqPublisher<TMessage>(_loggerFactory, s, submitTelemetry: false);

                    if (s.IsDurable && _publishingQueueRepository != null)
                        publisher.SetQueueRepository(_publishingQueueRepository);
                    else
                        publisher.DisableInMemoryQueuePersistence();

                    var result = publisher
                        .SetSerializer(serializer)
                        .SetWriteHeadersFunc(_correlationManager.BuildCorrelationHeadersIfExists);
                    result.Start();
                    return result;
                });
            }
        }

        /// <remarks>
        ///     ReSharper auto-generated
        /// </remarks>
        private sealed class SubscriptionSettingsEqualityComparer : IEqualityComparer<RabbitMqSubscriptionSettings>
        {
            public bool Equals(RabbitMqSubscriptionSettings x, RabbitMqSubscriptionSettings y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.ConnectionString, y.ConnectionString) &&
                       string.Equals(x.ExchangeName, y.ExchangeName);
            }

            public int GetHashCode(RabbitMqSubscriptionSettings obj)
            {
                unchecked
                {
                    return ((obj.ConnectionString != null ? obj.ConnectionString.GetHashCode() : 0) * 397) ^
                           (obj.ExchangeName != null ? obj.ExchangeName.GetHashCode() : 0);
                }
            }
        }
    }
}