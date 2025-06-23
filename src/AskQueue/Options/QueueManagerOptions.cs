public class QueueManagerOptions
{
    public TimeSpan AckTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public int MaxRetryCount { get; set; } = 3;
    public int MaxMessageSize { get; set; } = 1024 * 1024;
}