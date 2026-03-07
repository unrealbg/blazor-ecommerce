using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Infrastructure.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shipping.Application.Providers;
using Shipping.Application.Shipping;
using Shipping.Infrastructure.Persistence;
using Shipping.Infrastructure.Providers;
using Shipping.Infrastructure.Retention;
using Shipping.Infrastructure.Shipping;
using Shipping.Infrastructure.Webhooks;

namespace Shipping.Infrastructure.DependencyInjection;

public sealed class ShippingInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Shipping";

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.Configure<ShippingModuleOptions>(configuration.GetSection(ShippingModuleOptions.SectionName));
        services.Configure<DemoCarrierOptions>(configuration.GetSection(DemoCarrierOptions.SectionName));

        services.AddDbContext<ShippingDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "shipping")));

        services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
        services.AddScoped<IShippingZoneRepository, ShippingZoneRepository>();
        services.AddScoped<IShippingRateRuleRepository, ShippingRateRuleRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IShipmentEventRepository, ShipmentEventRepository>();
        services.AddScoped<ICarrierWebhookInboxRepository, CarrierWebhookInboxRepository>();
        services.AddScoped<BuildingBlocks.Infrastructure.Retention.IRetentionTask, ShippingWebhookRetentionTask>();
        services.AddScoped<IShippingUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<ShippingDbContext>());

        services.AddScoped<IShippingCarrierProvider, DemoCarrierProvider>();
        services.AddScoped<IShippingCarrierProviderFactory, ShippingCarrierProviderFactory>();
        services.AddScoped<IShippingWebhookVerifier, ShippingWebhookVerifier>();

        services.AddScoped<IShippingQuoteCalculator, ShippingQuoteService>();
        services.AddScoped<IShippingQuoteService, ShippingQuoteService>();
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShippingDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
