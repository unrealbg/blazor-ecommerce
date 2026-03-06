using BuildingBlocks.Infrastructure.Messaging;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
                               ?? "Host=localhost;Port=5432;Database=blazor_ecommerce;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);
        return new InventoryDbContext(optionsBuilder.Options, new SystemTextJsonEventSerializer());
    }
}
