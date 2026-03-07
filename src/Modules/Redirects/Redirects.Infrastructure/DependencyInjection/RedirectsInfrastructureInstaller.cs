using System.Net.Sockets;
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
            var redisConnection = TryConnectRedis(redisConnectionString);
            if (redisConnection is not null)
            {
                services.TryAddSingleton(redisConnection);
            }
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

    private static IConnectionMultiplexer? TryConnectRedis(string connectionString)
    {
        try
        {
            return ConnectionMultiplexer.Connect(BuildRedisOptions(connectionString));
        }
        catch (RedisConnectionException)
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private static ConfigurationOptions BuildRedisOptions(string connectionString)
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        options.ConnectRetry = 1;
        options.ConnectTimeout = 1000;
        options.SyncTimeout = 1000;
        return options;
    }
}
