using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class ShippingRateRuleConfiguration : IEntityTypeConfiguration<ShippingRateRule>
{
    public void Configure(EntityTypeBuilder<ShippingRateRule> builder)
    {
        builder.ToTable("shipping_rate_rules");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.MinOrderAmount).HasColumnType("numeric(18,2)");
        builder.Property(entity => entity.MaxOrderAmount).HasColumnType("numeric(18,2)");
        builder.Property(entity => entity.MinWeightKg).HasColumnType("numeric(18,3)");
        builder.Property(entity => entity.MaxWeightKg).HasColumnType("numeric(18,3)");
        builder.Property(entity => entity.PriceAmount).HasColumnType("numeric(18,2)");
        builder.Property(entity => entity.FreeShippingThresholdAmount).HasColumnType("numeric(18,2)");
        builder.Property(entity => entity.Currency)
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).IsRequired();
        builder.Property(entity => entity.RowVersion)
            .IsConcurrencyToken();

        builder.HasIndex(entity => new { entity.ShippingMethodId, entity.ShippingZoneId, entity.IsActive });
    }
}
