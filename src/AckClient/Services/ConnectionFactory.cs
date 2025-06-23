using System.Threading.Tasks;

public class ConnectionFactory
{
    private string _userName;

    private string _password;

    public string HostName
    {
        get;
        set;
    }

    public int Port { get; set; }

    public ConnectionFactory()
    {

    }

    public async Task<Connection> CreateConnection()
    {
        var connection = new Connection(this.HostName, this.Port);
        await connection.ConnectAsync();
        return connection;
    }
}