using Cart.Domain.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cart.Infrastructure.Persistence;

internal sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.ToTable("carts");

        builder.HasKey(cart => cart.Id);

        builder.Property(cart => cart.CustomerId)
            .IsRequired();

        builder.Property(cart => cart.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(cart => cart.CreatedOnUtc)
            .IsRequired();

        builder.Property(cart => cart.CheckedOutOnUtc);

        builder.OwnsOne(cart => cart.CheckoutTotal, totalBuilder =>
        {
            totalBuilder.Property(total => total.Currency)
                .HasColumnName("checkout_currency")
                .HasMaxLength(3);

            totalBuilder.Property(total => total.Amount)
                .HasColumnName("checkout_amount")
                .HasPrecision(18, 2);
        });
    }
}
