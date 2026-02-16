namespace OrderSystem.Domain.Entities;

using OrderSystem.Domain.ValueObjects;

public class OrderItem
{
    public Guid Id { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }

    internal OrderItem(string productName, int quantity, Money unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("El nombre del producto es requerido.");
        if (quantity <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.");

        Id = Guid.NewGuid();
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Money GetSubtotal() => UnitPrice.Multiply(Quantity);
}
