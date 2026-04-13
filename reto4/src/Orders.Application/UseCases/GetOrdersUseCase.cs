namespace Orders.Application.UseCases;

using Orders.Application.DTOs;
using Orders.Domain.Repositories;

public class GetOrdersUseCase
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<OrderResponse>> ExecuteAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return orders.Select(order =>
        {
            var items = order.Items.Select(i => new OrderItemResponse(
                i.Id, i.ProductName, i.Quantity, i.UnitPrice.Amount, i.GetSubtotal().Amount
            )).ToList();

            return new OrderResponse(
                order.Id, order.CustomerName, order.Status.ToString(),
                order.CreatedAt, items, order.CalculateTotal().Amount
            );
        });
    }

    public async Task<OrderResponse?> ExecuteByIdAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is null) return null;

        var items = order.Items.Select(i => new OrderItemResponse(
            i.Id, i.ProductName, i.Quantity, i.UnitPrice.Amount, i.GetSubtotal().Amount
        )).ToList();

        return new OrderResponse(
            order.Id, order.CustomerName, order.Status.ToString(),
            order.CreatedAt, items, order.CalculateTotal().Amount
        );
    }
}
