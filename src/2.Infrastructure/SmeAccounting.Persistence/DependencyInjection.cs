using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmeAccounting.Application.Interfaces;
using SmeAccounting.Persistence.Repositories;

namespace SmeAccounting.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing PostgreSQL connection string. Set ConnectionStrings__SmeAccounting env var.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__efmigrations", "sys")));

        services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
