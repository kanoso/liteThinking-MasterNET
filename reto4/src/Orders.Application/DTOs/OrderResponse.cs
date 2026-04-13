namespace Orders.Application.DTOs;

public record OrderResponse(
    Guid Id,
    string CustomerName,
    string Status,
    DateTime CreatedAt,
    List<OrderItemResponse> Items,
    decimal Total
);

public record OrderItemResponse(
    Guid Id,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);
