namespace Orders.Domain.Entities;

using Orders.Domain.ValueObjects;

public class OrderItem
{
    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;

    private OrderItem() { }

    public OrderItem(string productName, int quantity, Money unitPrice)
    {
        Id = Guid.NewGuid();
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Money GetSubtotal() => new Money(UnitPrice.Amount * Quantity, UnitPrice.Currency);
}
