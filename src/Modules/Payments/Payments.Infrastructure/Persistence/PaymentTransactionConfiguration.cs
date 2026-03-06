using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence;

internal sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.PaymentIntentId)
            .HasColumnName("payment_intent_id")
            .IsRequired();

        builder.Property(transaction => transaction.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(transaction => transaction.ProviderTransactionId)
            .HasColumnName("provider_transaction_id")
            .HasMaxLength(256);

        builder.Property(transaction => transaction.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(transaction => transaction.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(transaction => transaction.RawReference)
            .HasColumnName("raw_reference")
            .HasMaxLength(256);

        builder.Property(transaction => transaction.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(transaction => transaction.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType("text");

        builder.HasIndex(transaction => transaction.PaymentIntentId);
        builder.HasIndex(transaction => transaction.CreatedAtUtc);
    }
}
