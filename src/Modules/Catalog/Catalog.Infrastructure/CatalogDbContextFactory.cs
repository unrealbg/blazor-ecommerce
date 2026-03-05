using BuildingBlocks.Infrastructure.Messaging;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure;

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseNpgsql(
            GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "catalog"));

        return new CatalogDbContext(optionsBuilder.Options, new SystemTextJsonEventSerializer());
    }

    private static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
               ?? "Host=localhost;Port=5432;Database=blazor_ecommerce;Username=postgres;Password=postgres";
    }
}
