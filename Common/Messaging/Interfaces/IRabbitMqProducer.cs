namespace Common.Messaging.Interfaces;

public interface IRabbitMqProducer
{
    Task Publish<T>(T message);
}
