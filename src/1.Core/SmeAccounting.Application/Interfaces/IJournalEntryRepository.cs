using SmeAccounting.Domain.Entities;

namespace SmeAccounting.Application.Interfaces;

public interface IJournalEntryRepository
{
    Task AddAsync(JournalEntry entry, CancellationToken cancellationToken);
}
