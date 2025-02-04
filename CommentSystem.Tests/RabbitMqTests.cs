using System;
using System.Text;

using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CommentSystem.Config;

public class RabbitMqTests
{
    private readonly IOptions<AppOptions> _options;

    public RabbitMqTests()
    {
        // Mock the options
        _options = Options.Create(new AppOptions
        {
            ConnectionStrings = new ConnectionStringsOptions
            {
                DefaultConnection = "Server=localhost;Database=TestDB;User Id=SA;Password=TestPassword;"
            },
            Redis = new RedisOptions
            {
                Connection = "localhost:6379",
                InstanceName = "TestInstance"
            },
            RabbitMq = new RabbitMqOptions
            {
                HostName = "rabbitmq",
                UserName = "CommentSystemAdmin",
                Password = "CommentSystem!2025",
                Port = 5672,
                QueueName = "comments_queue"
            },
            CaptchaServiceUrl = "http://localhost:5002",
            IpRateLimit = 10
        });
    }

    [Fact]
    public async Task RabbitMQ_ShouldReceiveMessage()
    {
        // Arrange
        var factory = new ConnectionFactory
        {
            HostName = _options.Value.RabbitMq.HostName,
            UserName = _options.Value.RabbitMq.UserName,
            Password = _options.Value.RabbitMq.Password,
            Port = _options.Value.RabbitMq.Port
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Declare the queue
        await channel.QueueDeclareAsync(
            queue: _options.Value.RabbitMq.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var message = "Test message";
        var body = Encoding.UTF8.GetBytes(message);

        // Send the message
        await channel.BasicPublishAsync(exchange: "", routingKey: _options.Value.RabbitMq.QueueName, body: body);

        // Act: Receive the message
        var receivedMessageTcs = new TaskCompletionSource<string>();

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            receivedMessageTcs.TrySetResult(Encoding.UTF8.GetString(ea.Body.ToArray()));
            await Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(queue: _options.Value.RabbitMq.QueueName, autoAck: true, consumer: consumer);

        // Wait for the message to be received
        var receivedMessage = await receivedMessageTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal(message, receivedMessage);
    }
}
