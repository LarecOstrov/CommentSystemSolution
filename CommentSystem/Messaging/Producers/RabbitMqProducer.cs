using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using CommentSystem.Messaging.Interfaces;
using System.Threading.Tasks;

namespace CommentSystem.Messaging.Producers
{
    public class RabbitMqProducer : IRabbitMqProducer, IAsyncDisposable
    {
        private readonly IChannel _channel;

        public RabbitMqProducer(IConnection connection)
        {
            _channel = Task.Run(() => connection.CreateChannelAsync()).GetAwaiter().GetResult();
        }

        public async Task Publish<T>(string queueName, T message)
        {
            await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            await _channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
            }
        }
    }
}
