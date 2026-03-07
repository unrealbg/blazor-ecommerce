using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence;

internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");
        builder.HasKey(reservation => reservation.Id);

        builder.Property(reservation => reservation.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(reservation => reservation.VariantId)
            .HasColumnName("variant_id")
            .IsRequired();

        builder.Property(reservation => reservation.Sku)
            .HasColumnName("sku")
            .HasMaxLength(64);

        builder.Property(reservation => reservation.CartId)
            .HasColumnName("cart_id")
            .HasMaxLength(128);

        builder.Property(reservation => reservation.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(reservation => reservation.OrderId)
            .HasColumnName("order_id");

        builder.Property(reservation => reservation.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(reservation => reservation.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(reservation => reservation.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(reservation => reservation.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(reservation => reservation.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(reservation => reservation.ReservationToken)
            .HasColumnName("reservation_token")
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(reservation => reservation.ReservationToken)
            .HasDatabaseName("ux_stock_reservations_token")
            .IsUnique();

        builder.HasIndex(reservation => new { reservation.ProductId, reservation.Status })
            .HasDatabaseName("ix_stock_reservations_product_status");

        builder.HasIndex(reservation => new { reservation.VariantId, reservation.Status })
            .HasDatabaseName("ix_stock_reservations_variant_status");

        builder.HasIndex(reservation => new { reservation.CartId, reservation.Status })
            .HasDatabaseName("ix_stock_reservations_cart_status");

        builder.HasIndex(reservation => new { reservation.CustomerId, reservation.Status })
            .HasDatabaseName("ix_stock_reservations_customer_status");

        builder.HasIndex(reservation => new { reservation.OrderId, reservation.Status })
            .HasDatabaseName("ix_stock_reservations_order_status");

        builder.HasIndex(reservation => new { reservation.Status, reservation.ExpiresAtUtc })
            .HasDatabaseName("ix_stock_reservations_status_expires");
    }
}
