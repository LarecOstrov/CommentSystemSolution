using CommentSystem.Models.Inputs;
using CommentSystem.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CommentSystem.WebSockets
{
    public class WebSocketHub : Hub
    {
        private readonly ICommentService _commentService;

        public WebSocketHub(ICommentService commentService)
        {
            _commentService = commentService;
        }

        public async Task SendMessage(AddCommentInput input)
        {
            await _commentService.PublishCommentAsync(input);
        }
    }
}
