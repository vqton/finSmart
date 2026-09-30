using SmeAccounting.Application.Interfaces;
using SmeAccounting.Domain.Entities;

namespace SmeAccounting.Persistence.Repositories;

public sealed class JournalEntryRepository(AppDbContext db) : IJournalEntryRepository
{
    public async Task AddAsync(JournalEntry entry, CancellationToken cancellationToken)
    {
        await db.JournalEntries.AddAsync(entry, cancellationToken);
    }
}
