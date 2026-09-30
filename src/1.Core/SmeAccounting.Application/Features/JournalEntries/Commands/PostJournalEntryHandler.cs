using MediatR;
using SmeAccounting.Application.Common;
using SmeAccounting.Application.Features.JournalEntries.Dtos;
using SmeAccounting.Application.Interfaces;
using SmeAccounting.Domain.Entities;
using SmeAccounting.Domain.Exceptions;
using SmeAccounting.Domain.ValueObjects;

namespace SmeAccounting.Application.Features.JournalEntries.Commands;

public sealed class PostJournalEntryHandler : IRequestHandler<PostJournalEntryCommand, Result<JournalEntryDto>>
{
    private readonly IJournalEntryRepository _entries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountingPeriodChecker _periods;
    private readonly PostJournalEntryValidator _validator = new();

    public PostJournalEntryHandler(
        IJournalEntryRepository entries,
        IUnitOfWork unitOfWork,
        IAccountingPeriodChecker periods)
    {
        _entries = entries;
        _unitOfWork = unitOfWork;
        _periods = periods;
    }

    public async Task<Result<JournalEntryDto>> Handle(PostJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            string detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<JournalEntryDto>.Failure("VALIDATION.FAILED", detail);
        }

        if (await _periods.IsClosedAsync(request.CompanyId, request.PostingDate, cancellationToken))
        {
            return Result<JournalEntryDto>.Failure("ACCOUNT.CLOSED_PERIOD", "Period is closed for posting date.");
        }

        JournalEntry entry;
        try
        {
            entry = new JournalEntry(
                Guid.NewGuid(),
                request.CompanyId,
                new VoucherNo(request.VoucherNo),
                request.PostingDate,
                request.Lines.Select(l => new JournalLine(
                    new AccountCode(l.AccountCode),
                    new Money(l.DebitAmount, request.Currency),
                    new Money(l.CreditAmount, request.Currency),
                    l.Description)));

            entry.Post(isPeriodClosed: false);
        }
        catch (DomainException ex)
        {
            return Result<JournalEntryDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _entries.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<JournalEntryDto>.Success(new JournalEntryDto(
            entry.Id,
            entry.VoucherNo.Value,
            entry.PostingDate,
            entry.Status.ToString(),
            request.Currency,
            entry.Lines.Select(l => new JournalLineDto(
                l.AccountCode.Value, l.Debit.Amount, l.Credit.Amount, l.Description)).ToList()));
    }
}
