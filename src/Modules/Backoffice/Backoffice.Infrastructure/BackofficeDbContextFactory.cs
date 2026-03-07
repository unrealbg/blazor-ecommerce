using Backoffice.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Backoffice.Infrastructure;

public sealed class BackofficeDbContextFactory : IDesignTimeDbContextFactory<BackofficeDbContext>
{
    public BackofficeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BackofficeDbContext>();
        optionsBuilder.UseNpgsql(
            GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "backoffice"));

        return new BackofficeDbContext(optionsBuilder.Options, new SystemTextJsonEventSerializer());
    }

    private static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
               ?? "Host=localhost;Port=5432;Database=blazor_ecommerce;Username=postgres;Password=postgres";
    }
}
