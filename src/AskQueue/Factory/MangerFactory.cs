public class ManagerFactory
{
    private readonly QueueManagerOptions _options;

    public ManagerFactory(QueueManagerOptions queueManagerOptions)
    {
        this._options = queueManagerOptions;
    }

    public QueueManager CreateConnectionManager() => new QueueManager(_options);
}
