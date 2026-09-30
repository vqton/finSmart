namespace SmeAccounting.Application.Features.JournalEntries.Dtos;

public sealed record JournalLineDto(string AccountCode, decimal DebitAmount, decimal CreditAmount, string? Description);

public sealed record JournalEntryDto(
    Guid Id,
    string VoucherNo,
    DateOnly PostingDate,
    string Status,
    string Currency,
    IReadOnlyList<JournalLineDto> Lines);
