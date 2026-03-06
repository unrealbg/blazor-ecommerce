using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Infrastructure.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Search.Application.Search;
using Search.Infrastructure.Persistence;
using Search.Infrastructure.Search;

namespace Search.Infrastructure.DependencyInjection;

public sealed class SearchInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Search";

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<SearchDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "search")));

        services.AddScoped<ISearchProvider, PostgresSearchProvider>();
        services.AddScoped<IProductSearchIndexer, ProductSearchIndexer>();
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
