using Common.Models;
using Microsoft.AspNetCore.SignalR;

namespace Common.WebSockets;

public class WebSocketHub : Hub
{
    public async Task BroadcastComment(Comment comment)
    {

        await Clients.All.SendAsync("ReceiveComment", comment);
    }
}
