using System;
using System.Collections.Concurrent;
using System.Security.Principal;
using System.Threading.Channels;
using AckQueueServer.Interfaces;
using AckQueueServer.Models;

namespace AckQueueServer.Services;

public class AckQueue
{
    /// <summary>
    /// Очередь сообщений
    /// </summary>
    private readonly Channel<AckMessage> _channel = Channel.CreateUnbounded<AckMessage>();

    /// <summary>
    /// Хранилище непотдтвержденныхсообщений
    /// </summary>
    private readonly ConcurrentDictionary<Guid, InFlightMessage> _inFlight = new();

    private readonly HashSet<IMessageConsumer> _messageConsumers = new();

    /// <summary>
    /// 
    /// </summary>
    private readonly TimeSpan _ackTimeout;

    private readonly int _maxRetryCount;

    private readonly bool _autoAck;

    private readonly CancellationTokenSource _cts = new();

    private readonly Task _monitorTask;

    public AckQueue(TimeSpan ackTimeout, int MaxRetryCount, bool autoAck)
    {
        this._autoAck = autoAck;
        this._maxRetryCount = MaxRetryCount;
        this._ackTimeout = ackTimeout;
        this._monitorTask = Task.Run(() => MonitorUnackedMessagesAsync(_cts.Token));
    }

    public async Task EnqueAsync(byte[] Payload)
    {
        var message = new AckMessage(Payload);
        var inFlight = new InFlightMessage(message);
        this._inFlight.TryAdd(message.Id, inFlight);
        await _channel.Writer.WriteAsync(message);
    }

    public Task AckAsync(Guid messageId)
    {
        this._inFlight.TryRemove(messageId, out _);
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken token)
    {
        await foreach (var message in this._channel.Reader.ReadAllAsync(token))
        {
            foreach (var consumer in _messageConsumers)
            {
                _ = Task.Run(async () =>
                {
                    await consumer.RaiseAsync(new BasicEventArgs { Body = new ReadOnlyMemory<byte>(message.Payload) });
                }, token);
            }

            if (_autoAck)
            {
                await this.AckAsync(message.Id);
            }         
        }
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        await _monitorTask;
    }

    public void StartConsume(IMessageConsumer consumer)
    {
        _messageConsumers.Add(consumer);
    }

    public void StopConsume(IMessageConsumer consume)
    {
        _messageConsumers.Remove(consume);
    }

    public void StopAllConsume()
    {
        _messageConsumers.Clear();
    }

    private async Task MonitorUnackedMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var kvp in _inFlight.ToArray())
            {
                var inflight = kvp.Value;
                if (DateTime.UtcNow - inflight.LastDeliveryTime > _ackTimeout)
                {
                    inflight.LastDeliveryTime = DateTime.UtcNow;
                    inflight.DelivaryAttempts++;
                    Console.WriteLine($"Retrying message: {inflight.Message.Id}, attempt {inflight.DelivaryAttempts}");

                    await _channel.Writer.WriteAsync(inflight.Message, cancellationToken);
                }
            }

            await Task.Delay(1000, cancellationToken);
        }
    }
}
