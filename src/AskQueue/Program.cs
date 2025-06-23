
using System.Text;
using AckQueue.Services;

var cts = new CancellationTokenSource();
var options = new QueueManagerOptions();
var factory = new ManagerFactory(options);
var manager = factory.CreateConnectionManager();
await manager.QueueDeclareAsync("SUS", autoAck: true, cancellationToken: cts.Token);

var consumer = new Consumer(manager);
manager.ConsumeAsync("SUS", consumer, cts.Token);
consumer.Received += async (model, arg) =>
{
   Console.WriteLine($"Received message to consumer: {Encoding.UTF8.GetString(arg.Body.ToArray())}");
   await Task.Yield();
};

var consumer1 = new Consumer(manager);
manager.ConsumeAsync("SUS", consumer1, cts.Token);
consumer1.Received += async (model, args) =>
{
   Console.WriteLine($"Received message to consumer1: {Encoding.UTF8.GetString(args.Body.ToArray())}");
   await Task.Yield();
};

await manager.PublishAsync("SUS", Encoding.UTF8.GetBytes("Hello world!"));


Console.ReadLine();
