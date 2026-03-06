using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shipping.Application.Shipping;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

public sealed class ShippingDbContext(
    DbContextOptions<ShippingDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer), IShippingUnitOfWork
{
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();

    public DbSet<ShippingZone> ShippingZones => Set<ShippingZone>();

    public DbSet<ShippingRateRule> ShippingRateRules => Set<ShippingRateRule>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();

    public DbSet<CarrierWebhookInboxMessage> CarrierWebhookInboxMessages => Set<CarrierWebhookInboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("shipping");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShippingDbContext).Assembly);
    }
}
