namespace Orders.Contracts.Events;

public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerName,
    DateTime CreatedAt
);
