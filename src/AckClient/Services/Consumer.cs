using AckClient.Models;

namespace AckClient.Services;
public class Consumer
{
    public event Func<object, BasicEventArgs, Task>? Received;

    public Guid Guid { get; private set;}

    public Consumer()
    {
        Guid = Guid.NewGuid();
    }

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