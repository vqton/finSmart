namespace SmeAccounting.Application.Common;

public sealed record Result<T>(bool IsSuccess, T? Value, string? ErrorCode, string? ErrorMessage)
{
    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string errorCode, string errorMessage) => new(false, default, errorCode, errorMessage);
}
