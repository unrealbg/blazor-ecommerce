using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shipping.Infrastructure.Persistence;

namespace Shipping.Infrastructure;

public sealed class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    public ShippingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShippingDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("BLAZOR_ECOMMERCE_POSTGRES")
                               ?? "Host=localhost;Port=5432;Database=blazorecommerce;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "shipping"));

        return new ShippingDbContext(optionsBuilder.Options, new SystemTextJsonEventSerializer());
    }
}
