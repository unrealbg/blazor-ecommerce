using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure;

public sealed class ReviewsDbContextFactory : IDesignTimeDbContextFactory<ReviewsDbContext>
{
    public ReviewsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? "Host=localhost;Port=5432;Database=blazor-ecommerce;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ReviewsDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "reviews"));

        return new ReviewsDbContext(optionsBuilder.Options, new SystemTextJsonEventSerializer());
    }
}
