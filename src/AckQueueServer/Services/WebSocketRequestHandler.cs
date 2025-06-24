

using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AckQueueServer.Interfaces;
using AckQueueServer.Services;

public class WebSocketRequestHandler
{
    private static readonly ConcurrentDictionary<string, AckQueue> _queues = new();

    public WebSocketRequestHandler()
    {

    }

    public async Task HandleAsync(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        IMessageConsumer? consumer = null;
        AckQueue? subscribedQueue = null;
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var doc = JsonDocument.Parse(json).RootElement;
                var type = doc.GetProperty("type").GetString();

                switch (type)
                {
                    case "subscribe":
                        {
                            var queueName = doc.GetProperty("queue").GetString()!;
                            subscribedQueue = _queues.GetOrAdd(queueName, _ => new AckQueue(TimeSpan.FromSeconds(5), 3, false));
                            consumer = new WebSocketConsumer(
                                webSocket,
                                Guid.Parse(doc.GetProperty("consumerID").GetString()!),
                                queueName);
                            subscribedQueue.StartConsume(consumer);
                            _ = subscribedQueue.StartAsync(CancellationToken.None);

                            break;
                        }
                    case "publish":
                        {
                            var queueName = doc.GetProperty("queue").GetString()!;
                            var payload = doc.GetProperty("payload").GetBytesFromBase64()!;

                            var queue = _queues.GetOrAdd(queueName, _ => new AckQueue(TimeSpan.FromSeconds(5), 3, false));
                            await queue.EnqueAsync(payload);

                            break;
                        }
                    case "ack":
                        {
                            var id = doc.GetProperty("id").GetGuid();
                            foreach (var queue in _queues.Values)
                            {
                                await queue.AckAsync(id);
                            }
                            break;
                        }
                }
            }
        }
        finally
        {
            if (consumer != null)
            {
                subscribedQueue?.StopConsume(consumer);
            }
            if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }
}