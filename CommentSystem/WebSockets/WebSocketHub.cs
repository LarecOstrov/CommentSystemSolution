using Common.Models.Inputs;
using Common.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CommentSystem.WebSockets;

internal class WebSocketHub : Hub
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
