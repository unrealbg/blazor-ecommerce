using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
{
    public void Configure(EntityTypeBuilder<ShippingMethod> builder)
    {
        builder.ToTable("shipping_methods");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entity => entity.Name)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.Description)
            .HasMaxLength(1000);
        builder.Property(entity => entity.Provider)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(entity => entity.Type)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(entity => entity.BasePriceAmount)
            .HasColumnType("numeric(18,2)");
        builder.Property(entity => entity.Currency)
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).IsRequired();
        builder.Property(entity => entity.RowVersion)
            .IsConcurrencyToken();

        builder.HasIndex(entity => entity.Code)
            .IsUnique();
        builder.HasIndex(entity => new { entity.IsActive, entity.Priority });
    }
}
