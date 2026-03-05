using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(product => product.Slug)
            .IsRequired()
            .HasMaxLength(220);

        builder.Property(product => product.Description)
            .HasMaxLength(2000);

        builder.Property(product => product.IsActive)
            .IsRequired();

        builder.OwnsOne(product => product.Price, priceBuilder =>
        {
            priceBuilder.Property(price => price.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();

            priceBuilder.Property(price => price.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.HasIndex(product => product.Slug)
            .IsUnique();
    }
}
