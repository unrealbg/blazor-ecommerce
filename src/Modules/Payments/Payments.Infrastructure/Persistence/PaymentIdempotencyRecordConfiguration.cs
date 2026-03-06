using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence;

internal sealed class PaymentIdempotencyRecordConfiguration : IEntityTypeConfiguration<PaymentIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<PaymentIdempotencyRecord> builder)
    {
        builder.ToTable("payment_idempotency_records");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Operation)
            .HasColumnName("operation")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(record => record.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(record => record.PaymentIntentId)
            .HasColumnName("payment_intent_id")
            .IsRequired();

        builder.Property(record => record.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(record => new { record.Operation, record.IdempotencyKey }).IsUnique();
        builder.HasIndex(record => record.PaymentIntentId);
    }
}
