using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CarrierName)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(entity => entity.CarrierServiceCode)
            .HasMaxLength(100);
        builder.Property(entity => entity.TrackingNumber)
            .HasMaxLength(200);
        builder.Property(entity => entity.TrackingUrl)
            .HasMaxLength(1000);
        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(64);
        builder.Property(entity => entity.RecipientName)
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(entity => entity.RecipientPhone)
            .HasMaxLength(64);
        builder.Property(entity => entity.AddressSnapshotJson)
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(entity => entity.ShippingPriceAmount)
            .HasColumnType("numeric(18,2)");
        builder.Property(entity => entity.Currency)
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).IsRequired();
        builder.Property(entity => entity.RowVersion)
            .IsConcurrencyToken();

        builder.HasIndex(entity => entity.OrderId)
            .IsUnique();
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.TrackingNumber);
    }
}
