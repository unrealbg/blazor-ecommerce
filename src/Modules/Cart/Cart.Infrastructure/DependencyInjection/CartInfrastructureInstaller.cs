using BuildingBlocks.Infrastructure.Modules;
using Cart.Application.Carts;
using Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.Infrastructure.DependencyInjection;

public sealed class CartInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Cart";

    public IReadOnlyCollection<string> DependsOnModules => ["Catalog"];

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<CartDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "cart")));

        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<CartDbContext>());
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
