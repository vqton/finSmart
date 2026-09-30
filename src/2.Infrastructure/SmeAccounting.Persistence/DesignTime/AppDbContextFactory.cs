using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmeAccounting.Persistence.DesignTime;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Real connection via ConnectionStrings__SmeAccounting env var.
        // Localhost fallback exists only so `migrations add` works offline (never connects).
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SmeAccounting")
            ?? "Host=localhost;Database=fin_smart;Username=dev;Password=dev";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__efmigrations", "sys"))
            .Options;
        return new AppDbContext(options);
    }
}
