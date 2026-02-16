namespace OrderSystem.Application.UseCases;

using OrderSystem.Application.DTOs;
using OrderSystem.Domain.Entities;
using OrderSystem.Domain.Repositories;

public class CreateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse> ExecuteAsync(CreateOrderRequest request)
    {
        var order = Order.Create(request.CustomerName);
        await _orderRepository.AddAsync(order);

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
