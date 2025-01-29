using Microsoft.AspNetCore.SignalR;
using CommentSystem.Messaging.Interfaces;

namespace CommentSystem.WebSockets
{
    public class WebSocketHub : Hub
    {
        private readonly IRabbitMqProducer _rabbitMqProducer;
        private readonly string _queueName;

        public WebSocketHub(IRabbitMqProducer rabbitMqProducer, IConfiguration configuration)
        {
            _rabbitMqProducer = rabbitMqProducer;            
            _queueName = configuration["RabbitMQ:QueueName"] ?? "comments_queue";
        }

        public async Task SendMessage(string userName, string email, string? homePage, string text)
        {
            var commentData = new
            {
                UserName = userName,
                Email = email,
                HomePage = homePage,
                Text = text
            };

            // send to RabbitMQ
            _ =_rabbitMqProducer.Publish(_queueName, commentData);

            // Notify all clients
            await Clients.All.SendAsync("ReceiveMessage", userName, text);
        }
    }
}
