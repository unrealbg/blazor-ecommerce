using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Infrastructure.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Application.Pricing;
using Pricing.Domain.PriceLists;
using Pricing.Infrastructure.Persistence;
using Pricing.Infrastructure.Pricing;

namespace Pricing.Infrastructure.DependencyInjection;

public sealed class PricingInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Pricing";

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.Configure<PricingModuleOptions>(configuration.GetSection(PricingModuleOptions.SectionName));

        services.AddDbContext<PricingDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "pricing")));

        services.AddScoped<IPriceListRepository, PriceListRepository>();
        services.AddScoped<IVariantPriceRepository, VariantPriceRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<IPromotionRedemptionRepository, PromotionRedemptionRepository>();
        services.AddScoped<IPricingUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<PricingDbContext>());

        services.AddScoped<IVariantPricingService, PricingService>();
        services.AddScoped<ICartPricingService, PricingService>();
        services.AddScoped<IPricingManagementService, PricingManagementService>();
        services.AddScoped<IPricingRedemptionService, PricingRedemptionService>();
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.PriceLists.AnyAsync(cancellationToken))
        {
            return;
        }

        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var defaultPriceListResult = PriceList.Create(
            "Default EUR",
            "default",
            "EUR",
            isDefault: true,
            isActive: true,
            priority: 100,
            clock.UtcNow);

        if (defaultPriceListResult.IsFailure)
        {
            throw new InvalidOperationException(defaultPriceListResult.Error.Message);
        }

        await dbContext.PriceLists.AddAsync(defaultPriceListResult.Value, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
