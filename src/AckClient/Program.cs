// See https://aka.ms/new-console-template for more information
using System.Net.WebSockets;
using System.Text;


var factory = new ConnectionFactory()
{
    HostName = "localhost",
    Port = 5149
};

using var connection = await factory.CreateConnection();

await connection.SendAsync("{ \"type\": \"message\", \"id\": \"guid-here\", \"payload\": \"Hello\" }");

Console.ReadLine();
