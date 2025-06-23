using AckQueue.Interfaces;

namespace AckQueue.Services;
public class Consumer : IMessageConsumer
{
    private readonly QueueManager queueManager;
    public bool ack = false;

    public Consumer(QueueManager manager)
    {
        this.queueManager = manager;
    }

    public event Func<object, BasicEventArgs, Task>? Received;

    public async Task RaiseAsync(BasicEventArgs args)
    {
       if (Received == null)
            return;

        var handlers = Received.GetInvocationList();
        var tasks = new Task[handlers.Length];

        for (int i = 0; i < handlers.Length; i++)
        {
            var handler = (Func<object, BasicEventArgs, Task>)handlers[i];
            tasks[i] = handler(this, args);
        }

        await Task.WhenAll(tasks);
    }
}