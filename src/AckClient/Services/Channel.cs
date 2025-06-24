using AckClient.Models;

namespace AckClient.Services;

public class Channel : IDisposable, IAsyncDisposable
{
    private  Connection _connection;
    private bool _disposed;

    public Channel(Connection con)
    {
        this._connection = con;
    }

    public async Task QueueDeclareAsync(string queueName)
    {
        var message = new AckClient.Models.SocketMessage()
        {
            type = "create",
            queue = queueName
        };
        await this._connection.SendAsync(message);
    }

    public async Task ConsumeAsync(string queueName, Consumer consumer)
    {
        var message = new SocketMessage()
        {
            type = "subscribe",
            queue = queueName,
            consumerID = consumer.Guid.ToString()
        };
        this._connection.RegisterConsumer(queueName,consumer);
        await this._connection.SendAsync(message);
    }

    public async Task PublishAsync(string queueName, byte[] body)
    {
        var message = new SocketMessage
        {
            type = "publish",
            queue = queueName,
            payload = Convert.ToBase64String(body)
        };

        await this._connection.SendAsync(message);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed)
        {
            return;
        }
        
        if (disposing)
        {
            this._connection = null;
        }
        _disposed = true;
    }

    ~Channel()
    {
        Dispose(false);
    }
}