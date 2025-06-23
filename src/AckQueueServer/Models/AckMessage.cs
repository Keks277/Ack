
namespace AckQueueServer.Models;
/// <summary>
/// Класс описывающий сообщение
/// </summary>
public class AckMessage
{
    public Guid Id { get; }
    public byte[] Payload { get; set; }

    public AckMessage(byte[] payload)
    {
        this.Payload = payload;
        this.Id = Guid.NewGuid();
    }
}