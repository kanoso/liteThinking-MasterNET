namespace Orders.Application.UseCases;

using Orders.Application.DTOs;
using Orders.Application.Interfaces;
using Orders.Contracts.Events;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;

public class CreateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMessageBus _messageBus;

    public CreateOrderUseCase(IOrderRepository orderRepository, IMessageBus messageBus)
    {
        _orderRepository = orderRepository;
        _messageBus = messageBus;
    }

    public async Task<OrderResponse> ExecuteAsync(CreateOrderRequest request)
    {
        var order = Order.Create(request.CustomerName);
        await _orderRepository.AddAsync(order);

        await _messageBus.PublishAsync(
            new OrderCreatedEvent(order.Id, order.CustomerName, order.CreatedAt),
            queue: "order.created"
        );

        return MapToResponse(order);
    }

    private static OrderResponse MapToResponse(Order order)
    {
        var items = order.Items.Select(i => new OrderItemResponse(
            i.Id, i.ProductName, i.Quantity, i.UnitPrice.Amount, i.GetSubtotal().Amount
        )).ToList();

        return new OrderResponse(
            order.Id,
            order.CustomerName,
            order.Status.ToString(),
            order.CreatedAt,
            items,
            order.CalculateTotal().Amount
        );
    }
}
