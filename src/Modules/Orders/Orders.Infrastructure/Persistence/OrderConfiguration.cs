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

        builder.Property(order => order.CartId)
            .IsRequired();

        builder.Property(order => order.CustomerId)
            .IsRequired();

        builder.Property(order => order.CreatedOnUtc)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.OwnsOne(order => order.Total, totalBuilder =>
        {
            totalBuilder.Property(total => total.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();

            totalBuilder.Property(total => total.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();
        });
    }
}
