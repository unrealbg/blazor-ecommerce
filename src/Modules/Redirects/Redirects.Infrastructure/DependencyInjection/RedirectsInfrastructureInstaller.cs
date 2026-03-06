using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Infrastructure.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Redirects.Application.RedirectRules;
using Redirects.Infrastructure.Persistence;
using StackExchange.Redis;

namespace Redirects.Infrastructure.DependencyInjection;

public sealed class RedirectsInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Redirects";

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
                                       ?? throw new InvalidOperationException(
                                           "Connection string 'Postgres' is not configured.");

        services.AddDbContext<RedirectsDbContext>(options =>
            options.UseNpgsql(postgresConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "redirects")));

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        }

        services.AddMemoryCache();
        services.AddScoped<IRedirectRuleRepository, RedirectRuleRepository>();
        services.AddScoped<IRedirectRuleCache, RedirectRuleCache>();
        services.AddScoped<IRedirectLookupService, RedirectLookupService>();
        services.AddScoped<IRedirectsUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<RedirectsDbContext>());
        services.AddScoped<IRedirectRuleWriter, RedirectRuleWriter>();

        services.TryAddSingleton<RedirectHitQueue>();
        services.TryAddSingleton<IRedirectHitRecorder, RedirectHitRecorder>();
        services.AddHostedService<RedirectHitBackgroundService>();
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RedirectsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
