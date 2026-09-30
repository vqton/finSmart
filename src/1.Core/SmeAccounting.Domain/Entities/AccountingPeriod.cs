using SmeAccounting.Domain.Exceptions;

namespace SmeAccounting.Domain.Entities;

public sealed class AccountingPeriod
{
    public Guid Id { get; }
    public Guid CompanyId { get; }
    public DateOnly FromDate { get; }
    public DateOnly ToDate { get; }
    public bool IsClosed { get; private set; }

    public AccountingPeriod(Guid id, Guid companyId, DateOnly fromDate, DateOnly toDate)
    {
        if (id == Guid.Empty || companyId == Guid.Empty)
        {
            throw new DomainException("ACCOUNT.INVALID_PERIOD", "Period id and company id are required.");
        }

        if (toDate < fromDate)
        {
            throw new DomainException("ACCOUNT.INVALID_PERIOD", "Period end must be on or after start.");
        }

        Id = id;
        CompanyId = companyId;
        FromDate = fromDate;
        ToDate = toDate;
    }

    public bool Contains(DateOnly date) => date >= FromDate && date <= ToDate;

    public void Close() => IsClosed = true;
}
