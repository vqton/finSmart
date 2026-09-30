using SmeAccounting.Domain.Exceptions;

namespace SmeAccounting.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException("MONEY.NEGATIVE", "Amount must be non-negative.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new DomainException("MONEY.INVALID_CURRENCY", "Currency must be ISO4217 (3 letters).");
        }

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0m, currency);
}
