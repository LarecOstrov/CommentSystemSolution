using Common.Config;
using Common.Models.DTOs;
using Common.Services.Interfaces;
using Common.WebSockets;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;
using System.Text.Json;

internal class RabbitMqConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IHubContext<WebSocketHub> _hubContext;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _queueName;
    private readonly string _deadQueueName;
    private readonly string _deadExchangeName;
    private readonly AppOptions _options;

    public RabbitMqConsumer(IServiceProvider serviceProvider, IOptions<AppOptions> options,
        IConnectionFactory connectionFactory, IHubContext<WebSocketHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _connectionFactory = connectionFactory;
        _queueName = _options.RabbitMq.QueueName;
        _deadQueueName = _options.RabbitMq.DeadQueueName;
        _deadExchangeName = _options.RabbitMq.DeadExchangeName;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Log.Information("Starting RabbitMQ consumer...");

                // Create connection and channel
                _connection = await _connectionFactory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                // Declare Dead Letter Queue
                await DeadQueueDeclare();

                // Declare Main Queue with Dead Letter Exchange
                await _channel.QueueDeclareAsync(_queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object?>
                    {
                        { "x-dead-letter-exchange", _deadExchangeName }
                    });

                // Prefetch Count processing 1 message at a time
                await _channel.BasicQosAsync(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var commentData = JsonSerializer.Deserialize<CommentDto>(message);

                    if (commentData is null)
                    {
                        Log.Error($"Error while deserializing comment from RabbitMQ: {message}");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                        return;
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var commentService = scope.ServiceProvider.GetRequiredService<ISaveCommentService>();

                    try
                    {
                        var comment  = await commentService.AddCommentAsync(commentData);
                        
                        await _hubContext.Clients.All.SendAsync("ReceiveComment", comment);                        
                        
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error while processing comment from RabbitMQ");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                };

                await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer);
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

    private async Task DeadQueueDeclare()
    {
        ArgumentNullException.ThrowIfNull(_channel, nameof(_channel));

        await _channel.ExchangeDeclareAsync(_deadExchangeName, ExchangeType.Fanout, durable: true);

        await _channel.QueueDeclareAsync(_deadQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await _channel.QueueBindAsync(_deadQueueName, _deadExchangeName, "");
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        Log.Information("Stopping RabbitMQ consumer...");

        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel = null;
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection = null;
        }

        await base.StopAsync(stoppingToken);
    }
}
