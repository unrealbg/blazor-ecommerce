using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.PriceLists;

namespace Pricing.Infrastructure.Persistence;

internal sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("price_lists");
        builder.HasKey(priceList => priceList.Id);

        builder.Property(priceList => priceList.Name)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(priceList => priceList.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(priceList => priceList.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(priceList => priceList.Priority)
            .IsRequired();

        builder.Property(priceList => priceList.CreatedAtUtc)
            .IsRequired();

        builder.Property(priceList => priceList.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(priceList => priceList.Code)
            .IsUnique();

        builder.HasIndex(priceList => new { priceList.Currency, priceList.IsDefault });
    }
}
