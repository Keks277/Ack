namespace AckQueue.Interfaces;

public interface IMessageConsumer
{
    event Func<object, BasicEventArgs, Task> Received;
    Task RaiseAsync(BasicEventArgs args);
}