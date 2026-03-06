using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class ShipmentEventConfiguration : IEntityTypeConfiguration<ShipmentEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentEvent> builder)
    {
        builder.ToTable("shipment_events");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.EventType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entity => entity.Message)
            .HasMaxLength(1000);
        builder.Property(entity => entity.ExternalEventId)
            .HasMaxLength(200);
        builder.Property(entity => entity.MetadataJson)
            .HasColumnType("jsonb");

        builder.HasIndex(entity => new { entity.ShipmentId, entity.OccurredAtUtc });
        builder.HasIndex(entity => entity.ExternalEventId);
    }
}
