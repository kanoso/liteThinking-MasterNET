namespace OrderSystem.Application.DTOs;

public record OrderItemResponse(Guid Id, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);

public record OrderResponse(
    Guid Id,
    string CustomerName,
    string Status,
    DateTime CreatedAt,
    List<OrderItemResponse> Items,
    decimal Total
);
