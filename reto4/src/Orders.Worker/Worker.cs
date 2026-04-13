namespace Orders.Worker;

using System.Text;
using System.Text.Json;
using Orders.Contracts.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class OrderCreatedWorker : BackgroundService
{
    private readonly ILogger<OrderCreatedWorker> _logger;
    private readonly string _rabbitHost;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string Queue = "order.created";

    public OrderCreatedWorker(ILogger<OrderCreatedWorker> logger, IConfiguration config)
    {
        _logger = logger;
        _rabbitHost = config["RabbitMQ:Host"] ?? "localhost";
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { HostName = _rabbitHost };

        // Retry con backoff — RabbitMQ puede tardar unos segundos después del healthcheck
        var maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await _channel.QueueDeclareAsync(
                    queue: Queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Worker conectado a RabbitMQ ({Host}), escuchando cola '{Queue}'", _rabbitHost, Queue);
                break;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning("Intento {Attempt}/{Max} — no se pudo conectar a RabbitMQ: {Message}. Reintentando en 3s...",
                    attempt, maxRetries, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                var evento = JsonSerializer.Deserialize<OrderCreatedEvent>(body);

                if (evento is not null)
                {
                    _logger.LogInformation(
                        "[OrderCreatedWorker] Orden recibida — Id: {OrderId} | Cliente: {CustomerName} | Fecha: {CreatedAt}",
                        evento.OrderId,
                        evento.CustomerName,
                        evento.CreatedAt
                    );

                    // Aquí iría el envío real de email / push / SMS
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mensaje: {Body}", body);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel!.BasicConsumeAsync(queue: Queue, autoAck: false, consumer: consumer);

        // Mantiene el worker vivo hasta que se cancele
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
