// See https://aka.ms/new-console-template for more information
using System.Net.WebSockets;
using System.Text;
using AckClient.Services;


var factory = new ConnectionFactory()
{
    HostName = "localhost",
    Port = 5149
};

using var connection = await factory.CreateConnection();
using var channel =  connection.CreateChannel();
await channel.QueueDeclareAsync("SUS");
var consumer = new Consumer();
consumer.Received += async (model, args) =>
{
    Console.WriteLine($"Received {Encoding.UTF8.GetString(args.Body.ToArray())}");
};

await channel.ConsumeAsync("SUS", consumer);

await channel.PublishAsync("SUS", Encoding.UTF8.GetBytes("Hello World!"));


Console.ReadLine();
