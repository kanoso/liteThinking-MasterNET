namespace Orders.Application.Interfaces;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, string queue) where T : class;
}
