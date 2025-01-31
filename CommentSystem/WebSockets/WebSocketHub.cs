using Microsoft.AspNetCore.SignalR;
using CommentSystem.Messaging.Interfaces;
using CommentSystem.Models.Inputs;
using CommentSystem.Models.DTOs;

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

        public async Task SendMessage(AddCommentInput input)
        {
            var commentData = CommentDto.FromAddCommentInput(input);

            await _rabbitMqProducer.Publish(_queueName, commentData);
        }

        public async Task SendCommentUpdate(string userName, string text)
        {
            await Clients.All.SendAsync("ReceiveComment", userName, text);
        }
    }
}
