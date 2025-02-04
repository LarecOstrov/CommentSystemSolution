namespace Common.Messaging.Interfaces;

public interface IRabbitMqProducer
{
    Task Publish<T>(string queueName, T message);
}
