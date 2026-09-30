using System.Text.RegularExpressions;
using SmeAccounting.Domain.Exceptions;

namespace SmeAccounting.Domain.ValueObjects;

public sealed record AccountCode
{
    private static readonly Regex Pattern = new(@"^\d+(\.\d+)*$", RegexOptions.Compiled);

    public string Value { get; init; }

    public AccountCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Pattern.IsMatch(value.Trim()))
        {
            throw new InvalidAccountCodeException(value ?? string.Empty);
        }

        Value = value.Trim();
    }

    public override string ToString() => Value;
}
