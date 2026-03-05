using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.CustomerId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(order => order.PlacedAtUtc)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.OwnsOne(order => order.Subtotal, subtotalBuilder =>
        {
            subtotalBuilder.Property(total => total.Currency)
                .HasColumnName("subtotal_currency")
                .HasMaxLength(3)
                .IsRequired();

            subtotalBuilder.Property(total => total.Amount)
                .HasColumnName("subtotal_amount")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.OwnsOne(order => order.Total, totalBuilder =>
        {
            totalBuilder.Property(total => total.Currency)
                .HasColumnName("total_currency")
                .HasMaxLength(3)
                .IsRequired();

            totalBuilder.Property(total => total.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.OwnsMany(order => order.Lines, linesBuilder =>
        {
            linesBuilder.ToTable("order_lines");
            linesBuilder.WithOwner().HasForeignKey("order_id");

            linesBuilder.Property(line => line.ProductId)
                .HasColumnName("product_id")
                .IsRequired();

            linesBuilder.Property(line => line.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

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

            linesBuilder.HasKey("order_id", "ProductId");
        });

        builder.Navigation(order => order.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
