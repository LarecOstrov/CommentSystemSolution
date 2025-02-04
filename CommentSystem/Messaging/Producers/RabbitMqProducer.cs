using Common.Messaging.Interfaces;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client;
using Serilog;
using System.Text;
using System.Text.Json;

namespace CommentSystem.Messaging.Producers;   
internal class RabbitMqProducer : IRabbitMqProducer, IAsyncDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    private readonly int _retryDelayMs = 5000; // 5 seconds retry delay

    public RabbitMqProducer(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _ = Task.Run(ReconnectAsync); // Run reconnect in background
    }

    private async Task ReconnectAsync()
    {
        await _reconnectLock.WaitAsync(); // Ensure only one reconnect attempt at a time

        try
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
            {
                Log.Information("RabbitMQ connection already open.");
                return;
            }

            // Cleanup existing connection and channel
            await DisposeAsync();

            while (_connection == null || !_connection.IsOpen)
            {
                try
                {
                    Log.Warning("Reconnecting to RabbitMQ...");

                    _connection = await _connectionFactory.CreateConnectionAsync();
                    _channel = await _connection.CreateChannelAsync();

                    Log.Information("Successfully reconnected to RabbitMQ.");
                }
                catch (BrokerUnreachableException ex)
                {
                    Log.Error($"Failed to connect to RabbitMQ: {ex.Message}. Retrying in {_retryDelayMs / 1000} seconds...");
                    await Task.Delay(_retryDelayMs);
                }
                catch (Exception ex)
                {
                    Log.Error($"Unexpected error when connecting to RabbitMQ: {ex.Message}");
                    await Task.Delay(_retryDelayMs);
                }
            }
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    public async Task Publish<T>(string queueName, T message)
    {
        if (_channel == null || !_channel.IsOpen)
        {
            Log.Warning("RabbitMQ channel is closed. Trying to reconnect...");
            await ReconnectAsync();
        }

        await _channel!.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await _channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);
    }

    public async ValueTask DisposeAsync()
    {
        Log.Information("Disposing RabbitMqProducer...");
        if (_channel is not null && _channel.IsOpen)
        {
            await _channel.CloseAsync();
        }
        if (_connection is not null && _connection.IsOpen)
        {
            await _connection.CloseAsync();
        }
        _channel = null;
        _connection = null;
        Log.Information("RabbitMqProducer disposed.");
    }
}
