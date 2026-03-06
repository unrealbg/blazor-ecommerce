using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Infrastructure.Modules;
using Inventory.Application.Stock;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Stock;
using Inventory.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.DependencyInjection;

public sealed class InventoryInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Inventory";

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.Configure<InventoryModuleOptions>(configuration.GetSection(InventoryModuleOptions.SectionName));

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "inventory")));

        services.AddScoped<IStockItemRepository, StockItemRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IInventoryUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<InventoryDbContext>());

        services.AddScoped<IInventoryReservationService, InventoryReservationService>();
        services.AddScoped<IInventoryAvailabilityReader, InventoryAvailabilityReader>();
        services.AddScoped<IInventoryStockProvisioner, InventoryStockProvisioner>();

        services.AddHostedService<ReservationExpirationWorker>();
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
