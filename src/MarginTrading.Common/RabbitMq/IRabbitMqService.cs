// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;

namespace MarginTrading.Common.RabbitMq
{
    public interface IRabbitMqService
    {
        IMessageProducer<TMessage> GetProducer<TMessage>(RabbitMqPublisherConfiguration configuration,
            IRabbitMqSerializer<TMessage> serializer);

        IRabbitMqSerializer<TMessage> GetJsonSerializer<TMessage>();
    }
}