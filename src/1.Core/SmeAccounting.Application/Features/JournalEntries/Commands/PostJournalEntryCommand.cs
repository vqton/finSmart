using MediatR;
using SmeAccounting.Application.Common;
using SmeAccounting.Application.Features.JournalEntries.Dtos;

namespace SmeAccounting.Application.Features.JournalEntries.Commands;

public sealed record PostJournalEntryLine(string AccountCode, decimal DebitAmount, decimal CreditAmount, string? Description);

public sealed record PostJournalEntryCommand(
    Guid CompanyId,
    string VoucherNo,
    DateOnly PostingDate,
    string Currency,
    IReadOnlyList<PostJournalEntryLine> Lines) : IRequest<Result<JournalEntryDto>>;
