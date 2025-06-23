namespace AckQueueServer.Models;

public class BasicEventArgs
{
    public ReadOnlyMemory<byte> Body { get; set; }
}