using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(movement => movement.Sku)
            .HasColumnName("sku")
            .HasMaxLength(64);

        builder.Property(movement => movement.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(movement => movement.QuantityDelta)
            .HasColumnName("quantity_delta")
            .IsRequired();

        builder.Property(movement => movement.ReferenceId)
            .HasColumnName("reference_id");

        builder.Property(movement => movement.Reason)
            .HasColumnName("reason")
            .HasMaxLength(300);

        builder.Property(movement => movement.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(120);

        builder.Property(movement => movement.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(movement => new { movement.ProductId, movement.CreatedAtUtc })
            .HasDatabaseName("ix_stock_movements_product_created");
    }
}
