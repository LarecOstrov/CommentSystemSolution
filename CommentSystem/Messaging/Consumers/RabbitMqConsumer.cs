using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CommentSystem.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using CommentSystem.WebSockets;
using CommentSystem.Models.DTOs;

namespace CommentSystem.Messaging.Consumers
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IChannel _channel;
        private readonly string _queueName;

        public RabbitMqConsumer(IServiceProvider serviceProvider, IConfiguration configuration, IConnection connection)
        {
            _serviceProvider = serviceProvider;
            _queueName = configuration["RabbitMQ:QueueName"] ?? "comments_queue";

            _channel = Task.Run(() => connection.CreateChannelAsync()).GetAwaiter().GetResult();
            Task.Run(() => _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false)).GetAwaiter().GetResult();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var commentData = JsonSerializer.Deserialize<CommentDto>(message);

                using var scope = _serviceProvider.CreateScope();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<WebSocketHub>>();
                
                await hubContext.Clients.All.SendAsync("ReceiveComment", commentData.UserName, commentData.Text);
            };

            Task.Run(() => _channel.BasicConsumeAsync(_queueName, autoAck: true, consumer)).GetAwaiter().GetResult();
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
            }
        }
    }
}




