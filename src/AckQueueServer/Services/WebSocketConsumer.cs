using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AckQueueServer.Interfaces;
using AckQueueServer.Models;

namespace AckQueueServer.Services;
public class WebSocketConsumer : IMessageConsumer
{
    private readonly WebSocket _webSocket;
    public bool ack = false;

    public WebSocketConsumer(WebSocket webSocket)
    {
        this._webSocket = webSocket;
    }

    public event Func<object, BasicEventArgs, Task>? Received;

    public async Task RaiseAsync(BasicEventArgs args)
    {
        if (this._webSocket.State != WebSocketState.Open)
        {
            return;
        }

        var message = new
        {
            type = "message",
            body = Convert.ToBase64String(args.Body.ToArray())
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}