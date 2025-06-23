using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Text;

public class Connection : IDisposable, IAsyncDisposable
{
    private ClientWebSocket? _socket;
    private readonly CancellationTokenSource _cts;
    private readonly Uri _uri;
    private bool _disposed;

    public Connection(string hostname, int port)
    {
        this._cts = new CancellationTokenSource();
        this._uri = new Uri($"ws://{hostname}:{port}/ws");
        this._socket = new ClientWebSocket();
    }

    public async Task ConnectAsync()
    {
        if (this._socket != null)
        {
            await this._socket.ConnectAsync(this._uri, this._cts.Token);
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

    public async Task SendAsync(string message)
    {
        if (this._socket != null)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await this._socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }
    }

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
}