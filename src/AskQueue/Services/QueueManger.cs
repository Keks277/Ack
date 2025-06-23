using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using AckQueue.Interfaces;
using AckQueue.Services;

public class QueueManager
{
    private readonly ConcurrentDictionary<string, AckQueue.Services.AckQueue> _queues = new();

    private readonly QueueManagerOptions _options;

    public QueueManager(QueueManagerOptions options)
    {
        this._options = options;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="queueName">Имя очереди</param>
    /// <param name="autoDelete">Удалять после отписки всех consumers</param>
    /// <param name="maxLength">Макс количество сообщений</param>
    /// <param name="TTL">Время жизни сообщений</param>
    /// <returns></returns>
    public Task QueueDeclareAsync(
        string queueName,
        bool autoAck = false,
        bool autoDelete = false,
        int maxLength = 2000,
        int TTL = 60000,
        CancellationToken cancellationToken = default)
    {
        var queue = _queues.GetOrAdd(
            queueName,
            _ => new AckQueue.Services.AckQueue(_options.AckTimeout, _options.MaxRetryCount,autoAck));

        Task.Run(async () => await queue.StartAsync(cancellationToken)).ConfigureAwait(false);

        return Task.CompletedTask;
    }

    public async Task PublishAsync(string queueName, byte[] bytes)
    {
        if (!_queues.TryGetValue(queueName, out var queue))
        {
            throw new KeyNotFoundException("Queue not found");
        }

        await queue.EnqueAsync(bytes);
    }

    public void ConsumeAsync(
        string queueName,
        IMessageConsumer consumer,
        CancellationToken cancellationToken = default)
    {
        if (!_queues.TryGetValue(queueName, out var queue))
        {
            throw new KeyNotFoundException("Queue not found");
        }

        queue.StartConsume(consumer);
    }
}