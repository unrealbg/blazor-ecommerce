using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence;

internal sealed class PaymentIntentConfiguration : IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        builder.ToTable("payment_intents");

        builder.HasKey(paymentIntent => paymentIntent.Id);

        builder.Property(paymentIntent => paymentIntent.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(paymentIntent => paymentIntent.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(paymentIntent => paymentIntent.Provider)
            .HasColumnName("provider")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(paymentIntent => paymentIntent.ProviderPaymentIntentId)
            .HasColumnName("provider_payment_intent_id")
            .HasMaxLength(256);

        builder.Property(paymentIntent => paymentIntent.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(paymentIntent => paymentIntent.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(paymentIntent => paymentIntent.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(paymentIntent => paymentIntent.ClientSecret)
            .HasColumnName("client_secret")
            .HasMaxLength(512);

        builder.Property(paymentIntent => paymentIntent.FailureCode)
            .HasColumnName("failure_code")
            .HasMaxLength(128);

        builder.Property(paymentIntent => paymentIntent.FailureMessage)
            .HasColumnName("failure_message")
            .HasMaxLength(512);

        builder.Property(paymentIntent => paymentIntent.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(200);

        builder.Property(paymentIntent => paymentIntent.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(paymentIntent => paymentIntent.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(paymentIntent => paymentIntent.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.Property(paymentIntent => paymentIntent.RowVersion)
            .HasColumnName("row_version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(paymentIntent => paymentIntent.OrderId);
        builder.HasIndex(paymentIntent => paymentIntent.Status);
        builder.HasIndex(paymentIntent => new { paymentIntent.Provider, paymentIntent.ProviderPaymentIntentId })
            .IsUnique()
            .HasFilter("provider_payment_intent_id IS NOT NULL");
        builder.HasIndex(paymentIntent => paymentIntent.CreatedAtUtc);
    }
}
