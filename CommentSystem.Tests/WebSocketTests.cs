using Microsoft.AspNetCore.SignalR.Client;

public class WebSocketTests
{
    private readonly HubConnection _hubConnection;

    public WebSocketTests()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/ws")
            .Build();
    }

    [Fact]
    public async Task WebSocket_ShouldReceiveComment()
    {
        // Arrange
        await _hubConnection.StartAsync();
        string? receivedMessage = null;

        _hubConnection.On<string, string>("ReceiveComment", (user, text) =>
        {
            receivedMessage = text;
        });

        // Act
        await _hubConnection.InvokeAsync("SendComment", "TestUser", "Hello WebSocket!");

        await Task.Delay(1000);

        // Assert
        Assert.Equal("Hello WebSocket!", receivedMessage);
    }
}
