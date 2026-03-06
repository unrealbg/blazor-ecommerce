using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence;

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(item => item.Sku)
            .HasColumnName("sku")
            .HasMaxLength(64);

        builder.Property(item => item.OnHandQuantity)
            .HasColumnName("on_hand_quantity")
            .IsRequired();

        builder.Property(item => item.ReservedQuantity)
            .HasColumnName("reserved_quantity")
            .IsRequired();

        builder.Property(item => item.IsTracked)
            .HasColumnName("is_tracked")
            .IsRequired();

        builder.Property(item => item.AllowBackorder)
            .HasColumnName("allow_backorder")
            .IsRequired();

        builder.Property(item => item.RowVersion)
            .HasColumnName("row_version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(item => item.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(item => item.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(item => item.ProductId)
            .HasDatabaseName("ux_stock_items_product_id_without_sku")
            .IsUnique()
            .HasFilter("sku IS NULL");

        builder.HasIndex(item => new { item.ProductId, item.Sku })
            .HasDatabaseName("ux_stock_items_product_id_sku")
            .IsUnique()
            .HasFilter("sku IS NOT NULL");

        builder.HasIndex(item => item.IsTracked)
            .HasDatabaseName("ix_stock_items_is_tracked");
    }
}
