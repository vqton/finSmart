using SmeAccounting.Domain.Exceptions;
using SmeAccounting.Domain.ValueObjects;

namespace SmeAccounting.Domain.Entities;

public sealed class JournalLine
{
    public AccountCode AccountCode { get; private set; }
    public Money Debit { get; private set; }
    public Money Credit { get; private set; }
    public string? Description { get; private set; }

    // EF Core materialization only. Domain code must use the public constructor.
    private JournalLine()
    {
        AccountCode = null!;
        Debit = null!;
        Credit = null!;
    }

    public JournalLine(AccountCode accountCode, Money debit, Money credit, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(accountCode);
        ArgumentNullException.ThrowIfNull(debit);
        ArgumentNullException.ThrowIfNull(credit);

        if (!string.Equals(debit.Currency, credit.Currency, StringComparison.Ordinal))
        {
            throw new DomainException("GL.MIXED_CURRENCY", "Debit and credit currency must match within a line.");
        }

        if (debit.Amount > 0 && credit.Amount > 0)
        {
            throw new DomainException("GL.BOTH_SIDES", "A line must be debit-only or credit-only, not both.");
        }

        if (debit.Amount == 0 && credit.Amount == 0)
        {
            throw new DomainException("GL.ZERO_LINE", "A line must have a non-zero debit or credit.");
        }

        AccountCode = accountCode;
        Debit = debit;
        Credit = credit;
        Description = description;
    }
}
