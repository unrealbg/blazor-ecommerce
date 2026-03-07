using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pricing.Application.Pricing;
using Pricing.Domain.Coupons;
using Pricing.Domain.PriceLists;
using Pricing.Domain.Promotions;
using Pricing.Domain.Redemptions;
using Pricing.Domain.VariantPrices;

namespace Pricing.Infrastructure.Persistence;

public sealed class PricingDbContext(
    DbContextOptions<PricingDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer), IPricingUnitOfWork
{
    public DbSet<PriceList> PriceLists => Set<PriceList>();

    public DbSet<VariantPrice> VariantPrices => Set<VariantPrice>();

    public DbSet<Promotion> Promotions => Set<Promotion>();

    public DbSet<Coupon> Coupons => Set<Coupon>();

    public DbSet<PromotionRedemption> PromotionRedemptions => Set<PromotionRedemption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pricing");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PricingDbContext).Assembly);
    }
}
