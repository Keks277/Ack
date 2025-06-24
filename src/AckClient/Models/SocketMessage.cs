using System.ComponentModel.DataAnnotations;
namespace AckClient.Models;

public record SocketMessage
{
    public string type { get; init; } = null!;

    public string? queue { get; init; } = null;

    public string? payload { get; init; } = null;

    public Guid? id { get; init; } = null;

    public bool? require { get; init; } = null;

    public string? consumerID { get; init; } = null;
}