using SmeAccounting.Domain.Exceptions;

namespace SmeAccounting.Domain.ValueObjects;

public sealed record VoucherNo
{
    public string Value { get; init; }

    public VoucherNo(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 30)
        {
            throw new DomainException("VOUCHER.INVALID_NO", "VoucherNo is required, max 30 chars.");
        }

        Value = value.Trim();
    }

    public override string ToString() => Value;
}
