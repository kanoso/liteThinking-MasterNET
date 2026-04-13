namespace Orders.Application.UseCases;

using Orders.Application.DTOs;
using Orders.Domain.Repositories;
using Orders.Domain.ValueObjects;

public class AddItemToOrderUseCase
{
    private readonly IOrderRepository _orderRepository;

    public AddItemToOrderUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse> ExecuteAsync(Guid orderId, AddItemRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException($"Orden {orderId} no encontrada.");

        order.AddItem(request.ProductName, request.Quantity, new Money(request.UnitPrice, request.Currency));
        await _orderRepository.UpdateAsync(order);

        var items = order.Items.Select(i => new OrderItemResponse(
            i.Id, i.ProductName, i.Quantity, i.UnitPrice.Amount, i.GetSubtotal().Amount
        )).ToList();

        return new OrderResponse(
            order.Id, order.CustomerName, order.Status.ToString(),
            order.CreatedAt, items, order.CalculateTotal().Amount
        );
    }
}
