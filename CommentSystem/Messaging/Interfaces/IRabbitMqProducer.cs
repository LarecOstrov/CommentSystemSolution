namespace Common.Messaging.Interfaces;

internal interface IRabbitMqProducer
{
    Task Publish<T>(string queueName, T message);
}
