namespace Orders.Infrastructure.Messaging;

using System.Text;
using System.Text.Json;
using Orders.Application.Interfaces;
using RabbitMQ.Client;

public class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private RabbitMqMessageBus(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<RabbitMqMessageBus> CreateAsync(string host)
    {
        var factory = new ConnectionFactory { HostName = host };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        return new RabbitMqMessageBus(connection, channel);
    }

    public async Task PublishAsync<T>(T message, string queue) where T : class
    {
        await _channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queue,
            body: body
        );
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
