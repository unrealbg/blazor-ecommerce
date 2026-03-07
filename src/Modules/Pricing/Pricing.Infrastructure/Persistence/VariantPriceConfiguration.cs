using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.VariantPrices;

namespace Pricing.Infrastructure.Persistence;

internal sealed class VariantPriceConfiguration : IEntityTypeConfiguration<VariantPrice>
{
    public void Configure(EntityTypeBuilder<VariantPrice> builder)
    {
        builder.ToTable("variant_prices");
        builder.HasKey(variantPrice => variantPrice.Id);

        builder.Property(variantPrice => variantPrice.PriceListId)
            .HasColumnName("price_list_id")
            .IsRequired();

        builder.Property(variantPrice => variantPrice.VariantId)
            .HasColumnName("variant_id")
            .IsRequired();

        builder.Property(variantPrice => variantPrice.BasePriceAmount)
            .HasColumnName("base_price_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(variantPrice => variantPrice.CompareAtPriceAmount)
            .HasColumnName("compare_at_price_amount")
            .HasPrecision(18, 2);

        builder.Property(variantPrice => variantPrice.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(variantPrice => variantPrice.ValidFromUtc)
            .HasColumnName("valid_from_utc");

        builder.Property(variantPrice => variantPrice.ValidToUtc)
            .HasColumnName("valid_to_utc");

        builder.Property(variantPrice => variantPrice.CreatedAtUtc)
            .IsRequired();

        builder.Property(variantPrice => variantPrice.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(variantPrice => new { variantPrice.VariantId, variantPrice.PriceListId, variantPrice.IsActive });
    }
}
