using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Search.Infrastructure.Persistence;

namespace Search.Infrastructure;

public sealed class SearchDbContextFactory : IDesignTimeDbContextFactory<SearchDbContext>
{
    public SearchDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SearchDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("BLAZOR_ECOMMERCE_POSTGRES")
                               ?? "Host=localhost;Port=5432;Database=blazor-ecommerce;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "search"));

        return new SearchDbContext(optionsBuilder.Options, new SystemTextJsonEventSerializer());
    }
}
