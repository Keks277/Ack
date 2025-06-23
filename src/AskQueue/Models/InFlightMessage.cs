

namespace AckQueue.Models;

public class InFlightMessage
{
    public AckMessage Message { get; set; }

    public DateTime LastDeliveryTime { get; set; }

    public int DelivaryAttempts { get; set; }

    public InFlightMessage(AckMessage message)
    {
        this.Message = message;
        this.LastDeliveryTime = DateTime.UtcNow;
        this.DelivaryAttempts = 1;
    }
}