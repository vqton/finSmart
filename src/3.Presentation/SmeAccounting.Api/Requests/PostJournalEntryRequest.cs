using SmeAccounting.Application.Features.JournalEntries.Commands;

namespace SmeAccounting.Api.Requests;

public sealed record PostJournalEntryLineRequest(string AccountCode, decimal DebitAmount, decimal CreditAmount, string? Description);

public sealed record PostJournalEntryRequest(
    Guid CompanyId,
    string VoucherNo,
    DateOnly PostingDate,
    string Currency,
    IReadOnlyList<PostJournalEntryLineRequest> Lines)
{
    public PostJournalEntryCommand ToCommand() => new(
        CompanyId,
        VoucherNo,
        PostingDate,
        Currency,
        Lines.Select(l => new PostJournalEntryLine(l.AccountCode, l.DebitAmount, l.CreditAmount, l.Description)).ToList());
}
