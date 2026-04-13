namespace Orders.Domain.ValueObjects;

public record Money(decimal Amount, string Currency)
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("No se pueden sumar monedas distintas.");

        return new Money(Amount + other.Amount, Currency);
    }
}
