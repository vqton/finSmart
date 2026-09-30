namespace SmeAccounting.Application.Interfaces;

public interface IAccountingPeriodChecker
{
    Task<bool> IsClosedAsync(Guid companyId, DateOnly postingDate, CancellationToken cancellationToken);
}
