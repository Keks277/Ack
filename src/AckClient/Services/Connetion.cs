using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AckClient.Models;

namespace AckClient.Services;

public class Connection : IDisposable, IAsyncDisposable
{
    private ClientWebSocket? _socket;
    private readonly CancellationTokenSource _cts;
    private readonly Uri _uri;
    private bool _disposed;
    private readonly ConcurrentDictionary<string,HashSet<Consumer>> _queueConsumers = new();

    private Task? _receiveLoopTask;

    public Connection(string hostname, int port)
    {
        this._cts = new CancellationTokenSource();
        this._uri = new Uri($"ws://{hostname}:{port}/ws");
        this._socket = new ClientWebSocket();
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[1024 * 4];

        while (!_cts.IsCancellationRequested && this._socket?.State == WebSocketState.Open)
        {
            var result = await this._socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                _cts.Cancel();
                break;
            }

            var json = Encoding.UTF8.GetString(buffer,0,result.Count);
            var doc = JsonDocument.Parse(json).RootElement;
            var type = doc.GetProperty("type").GetString();

            if (!string.IsNullOrEmpty(type))
            {
                if (type == "message")
                {
                    var queueName = doc.GetProperty("queue").GetString();
                    var consumerId = doc.GetProperty("consumerID").GetString();
                    if (_queueConsumers.TryGetValue(queueName, out var consumers))
                    {
                        var message = doc.GetProperty("body").GetBytesFromBase64();
                        foreach (var consumer in consumers.Where(c => c.Guid.ToString().Equals(consumerId)))
                        {
                            _ = Task.Run(async () =>
                                await consumer.RaiseAsync(
                                    new BasicEventArgs
                                    {
                                        Body = new ReadOnlyMemory<byte>(message)
                                    }));
                        }
                    }
                }
            }
        }
    }

    public void RegisterConsumer(string queueName, Consumer consumer)
    {
        var consumers = _queueConsumers.GetOrAdd(queueName, _ => new HashSet<Consumer> { consumer });
        consumers.Add(consumer);
    }

    public Channel CreateChannel()
    {
        if (this._socket == null || this._socket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("Socket is not connected");
        }

        return new Channel(this);
    }

    public async Task ConnectAsync()
    {
        if (this._socket != null)
        {
            await this._socket.ConnectAsync(this._uri, this._cts.Token);
            _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(), _cts.Token);
        }
    }

    public async Task CloseAsync()
    {
        _cts.Cancel();

        if (this._socket != null)
        {
            await this._socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close from client", _cts.Token);
        }
    }

    public async Task SendAsync(SocketMessage message)
    {
        if (this._socket != null)
        {
            var json = JsonSerializer.Serialize<SocketMessage>(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await this._socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }
    }


    #region Dispose
    public async ValueTask DisposeAsync()
    {
        if (this._disposed)
        {
            return;
        }

        if (_socket?.State != WebSocketState.Closed)
        {
            await this.CloseAsync();
            this._socket?.Dispose();
            this._socket = null;
        }

        _cts.Cancel();
        if (this._receiveLoopTask != null)
        {
            await _receiveLoopTask;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Синхронное освобождение ресурсов
    /// </summary>
    /// <param name="disposing">Освобождаем управляемые ресурсы</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {

        }
        if (this._socket != null)
        {
            try
            {
                this._socket.Abort();
            }
            catch
            {

            }

            this._socket.Dispose();
            this._socket = null;
        }
        _disposed = true;
    }

    ~Connection()
    {
        Dispose(false);
    }
    #endregion
}