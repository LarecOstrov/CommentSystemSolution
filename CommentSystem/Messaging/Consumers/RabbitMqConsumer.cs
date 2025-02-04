using CommentSystem.Config;
using CommentSystem.WebSockets;
using Common.Models.DTOs;
using Common.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;
using System.Text.Json;

namespace CommentSystem.Messaging.Consumers;

internal class RabbitMqConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private IChannel? _channel;
    private readonly string _queueName;
    private readonly AppOptions _options;
    private readonly IConnection _connection;

    public RabbitMqConsumer(IServiceProvider serviceProvider, IOptions<AppOptions> options, IConnection connection)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _queueName = _options.RabbitMq.QueueName;
        _connection = connection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Log.Information("Starting RabbitMQ consumer...");

                _channel = await _connection.CreateChannelAsync();
                await _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var commentData = JsonSerializer.Deserialize<CommentDto>(message);

                    if (commentData is null)
                    {
                        Log.Error($"Error while deserializing comment from RabbitMQ : {message}");
                        return;
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var commentService = scope.ServiceProvider.GetRequiredService<ICommentService>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<WebSocketHub>>();

                    try
                    {
                        await commentService.AddCommentAsync(commentData);
                        await hubContext.Clients.All.SendAsync("ReceiveComment", commentData.UserName, commentData.Text);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error while processing comment from RabbitMQ");
                    }
                };

                await _channel.BasicConsumeAsync(_queueName, autoAck: true, consumer);
                Log.Information("RabbitMQ consumer started.");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in RabbitMQ consumer. Retrying in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        Log.Information("Stopping RabbitMQ consumer...");

        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel = null;
        }

        await base.StopAsync(stoppingToken);
    }
}

