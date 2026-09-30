namespace SmeAccounting.Domain.Exceptions;

public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

public sealed class UnbalancedEntryException : DomainException
{
    public UnbalancedEntryException(decimal debits, decimal credits)
        : base("GL.UNBALANCED", $"Debits ({debits}) must equal credits ({credits}).")
    {
    }
}

public sealed class ClosedPeriodException : DomainException
{
    public ClosedPeriodException(DateOnly postingDate)
        : base("ACCOUNT.CLOSED_PERIOD", $"Posting date {postingDate:yyyy-MM-dd} falls in a closed period.")
    {
    }
}

public sealed class InvalidAccountCodeException : DomainException
{
    public InvalidAccountCodeException(string code)
        : base("COA.INVALID_CODE", $"Account code '{code}' violates TT99 format (digits, dot-separated).")
    {
    }
}
