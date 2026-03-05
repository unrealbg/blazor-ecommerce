using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Persistence;

internal sealed class CheckoutIdempotencyConfiguration : IEntityTypeConfiguration<CheckoutIdempotency>
{
    public void Configure(EntityTypeBuilder<CheckoutIdempotency> builder)
    {
        builder.ToTable("checkout_idempotency");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(record => record.CustomerId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(record => record.OrderId)
            .IsRequired();

        builder.Property(record => record.CreatedOnUtc)
            .IsRequired();

        builder.HasIndex(record => record.IdempotencyKey)
            .IsUnique();
    }
}
