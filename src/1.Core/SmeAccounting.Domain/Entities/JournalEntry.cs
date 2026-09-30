using SmeAccounting.Domain.Events;
using SmeAccounting.Domain.Exceptions;
using SmeAccounting.Domain.ValueObjects;

namespace SmeAccounting.Domain.Entities;

public enum JournalEntryStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class JournalEntry
{
    private readonly List<JournalLine> _lines;
    private readonly List<object> _domainEvents = [];

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public VoucherNo VoucherNo { get; private set; }
    public DateOnly PostingDate { get; private set; }
    public JournalEntryStatus Status { get; private set; }
    public IReadOnlyList<JournalLine> Lines => _lines.AsReadOnly();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    // EF Core materialization only. Domain code must use the public constructor.
    private JournalEntry()
    {
        _lines = [];
        VoucherNo = null!;
    }

    public JournalEntry(Guid id, Guid companyId, VoucherNo voucherNo, DateOnly postingDate, IEnumerable<JournalLine> lines)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("GL.INVALID_ID", "Entry id is required.");
        }

        if (companyId == Guid.Empty)
        {
            throw new DomainException("GL.INVALID_COMPANY", "Company id is required.");
        }

        ArgumentNullException.ThrowIfNull(voucherNo);
        ArgumentNullException.ThrowIfNull(lines);

        _lines = lines.ToList();
        if (_lines.Count == 0)
        {
            throw new DomainException("GL.EMPTY_LINES", "Entry must have at least one line.");
        }

        string currency = _lines[0].Debit.Currency;
        if (_lines.Any(l => l.Debit.Currency != currency))
        {
            throw new DomainException("GL.MIXED_CURRENCY", "All lines must share one currency.");
        }

        Id = id;
        CompanyId = companyId;
        VoucherNo = voucherNo;
        PostingDate = postingDate;
        Status = JournalEntryStatus.Draft;
    }

    public decimal TotalDebits => _lines.Sum(l => l.Debit.Amount);

    public decimal TotalCredits => _lines.Sum(l => l.Credit.Amount);

    public void Post(bool isPeriodClosed)
    {
        if (Status != JournalEntryStatus.Draft)
        {
            throw new DomainException("GL.INVALID_STATUS", "Only draft entries can be posted.");
        }

        if (isPeriodClosed)
        {
            throw new ClosedPeriodException(PostingDate);
        }

        if (TotalDebits != TotalCredits)
        {
            throw new UnbalancedEntryException(TotalDebits, TotalCredits);
        }

        Status = JournalEntryStatus.Posted;
        _domainEvents.Add(new JournalEntryPostedEvent(Id, VoucherNo.Value, PostingDate));
    }

    public void Void()
    {
        if (Status != JournalEntryStatus.Posted)
        {
            throw new DomainException("GL.INVALID_STATUS", "Only posted entries can be voided.");
        }

        Status = JournalEntryStatus.Voided;
    }
}
