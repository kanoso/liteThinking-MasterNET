namespace Orders.Domain.Entities;

using Orders.Domain.Enums;
using Orders.Domain.ValueObjects;

public class Order
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(string customerName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("El nombre del cliente es requerido.");

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddItem(string productName, int quantity, Money unitPrice)
    {
        var item = new OrderItem(productName, quantity, unitPrice);
        _items.Add(item);
    }

    public Money CalculateTotal()
    {
        if (!_items.Any())
            return new Money(0, "S/.");

        return _items
            .Select(i => i.GetSubtotal())
            .Aggregate((a, b) => a.Add(b));
    }
}
