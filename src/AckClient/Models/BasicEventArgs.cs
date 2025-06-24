namespace AckClient.Models;

public class BasicEventArgs
{
    public ReadOnlyMemory<byte> Body { get; set; }
}