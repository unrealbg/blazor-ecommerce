using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Application.Operations;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Operations;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.Configure<OutboxDispatcherOptions>(configuration.GetSection(OutboxDispatcherOptions.SectionName));
        services.Configure<RetentionOptions>(configuration.GetSection(RetentionOptions.SectionName));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IEventSerializer, SystemTextJsonEventSerializer>();
        services.AddSingleton<IOperationalStateRegistry, OperationalStateRegistry>();
        services.AddSingleton<IBackgroundJobMonitor, BackgroundJobMonitor>();
        services.AddSingleton<OperationalMetricsObserver>();
        services.AddSingleton<IOperationalAlertSink, LoggingOperationalAlertSink>();

        services.AddDbContext<OutboxDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "shared")));

        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<IRetentionTask, ProcessedOutboxRetentionTask>();
        services.AddHostedService<OutboxDispatcherBackgroundService>();
        services.AddHostedService<OperationalMetricsInitializerHostedService>();
        services.AddHostedService<RetentionCleanupBackgroundService>();

        return services;
    }

    public static async Task InitializeSharedInfrastructureAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var outboxDbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        await outboxDbContext.Database.MigrateAsync(cancellationToken);
    }
}
