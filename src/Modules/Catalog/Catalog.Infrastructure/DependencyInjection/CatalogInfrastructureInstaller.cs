using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Infrastructure.Modules;
using Catalog.Application.Brands;
using Catalog.Application.Categories;
using Catalog.Application.Products;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure.DependencyInjection;

public sealed class CatalogInfrastructureInstaller : IModuleInfrastructureInstaller, IModuleInfrastructureSeeder
{
    public string ModuleName => "Catalog";

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "catalog")));

        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductListCache, ProductListCache>();
        services.AddScoped<IProductCatalogReader, ProductCatalogReader>();
        services.AddScoped<ICatalogUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<CatalogDbContext>());
        services.AddScoped<CatalogReleaseSeeder>();
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public async Task SeedAsync(IServiceProvider serviceProvider, string seedMode, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<CatalogReleaseSeeder>();
        await seeder.SeedAsync(seedMode, cancellationToken);
    }
}
