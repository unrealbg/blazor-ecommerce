using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CartAggregate = Cart.Domain.Carts.Cart;

namespace Cart.Infrastructure.Persistence;

internal sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<CartAggregate>
{
    public void Configure(EntityTypeBuilder<CartAggregate> builder)
    {
        builder.ToTable("carts");

        builder.HasKey(cart => cart.Id);

        builder.Property(cart => cart.CustomerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(cart => cart.RowVersion)
            .HasColumnName("row_version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(cart => cart.CustomerId)
            .IsUnique();

        builder.OwnsMany(cart => cart.Lines, linesBuilder =>
        {
            linesBuilder.ToTable("cart_lines");
            linesBuilder.WithOwner().HasForeignKey("cart_id");

            linesBuilder.Property(line => line.ProductId)
                .HasColumnName("product_id")
                .IsRequired();

            linesBuilder.Property(line => line.VariantId)
                .HasColumnName("variant_id")
                .IsRequired();

            linesBuilder.Property(line => line.Sku)
                .HasColumnName("sku")
                .HasMaxLength(64);

            linesBuilder.Property(line => line.ProductName)
                .HasColumnName("product_name")
                .HasMaxLength(200)
                .IsRequired();

            linesBuilder.Property(line => line.VariantName)
                .HasColumnName("variant_name")
                .HasMaxLength(200);

            linesBuilder.Property(line => line.SelectedOptionsJson)
                .HasColumnName("selected_options_json")
                .HasMaxLength(4000);

            linesBuilder.Property(line => line.ImageUrl)
                .HasColumnName("image_url")
                .HasMaxLength(2000);

            linesBuilder.Property(line => line.Quantity)
                .HasColumnName("quantity")
                .IsRequired();

            linesBuilder.OwnsOne(line => line.UnitPrice, moneyBuilder =>
            {
                moneyBuilder.Property(money => money.Currency)
                    .HasColumnName("unit_currency")
                    .HasMaxLength(3)
                    .IsRequired();

                moneyBuilder.Property(money => money.Amount)
                    .HasColumnName("unit_amount")
                    .HasPrecision(18, 2)
                    .IsRequired();
            });

            linesBuilder.HasKey("cart_id", "VariantId");
        });

        builder.Navigation(cart => cart.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
