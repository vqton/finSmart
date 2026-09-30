namespace SmeAccounting.Domain.Events;

public sealed record JournalEntryPostedEvent(Guid EntryId, string VoucherNo, DateOnly PostingDate);
