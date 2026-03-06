using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Payments.Domain.Payments;

public sealed class PaymentTransaction : Entity<Guid>
{
    private PaymentTransaction()
    {
    }

    private PaymentTransaction(
        Guid id,
        Guid paymentIntentId,
        PaymentTransactionType type,
        string? providerTransactionId,
        decimal amount,
        string currency,
        string status,
        string? rawReference,
        string? metadataJson,
        DateTime createdAtUtc)
    {
        Id = id;
        PaymentIntentId = paymentIntentId;
        Type = type;
        ProviderTransactionId = string.IsNullOrWhiteSpace(providerTransactionId) ? null : providerTransactionId.Trim();
        Amount = amount;
        Currency = currency;
        Status = status;
        RawReference = string.IsNullOrWhiteSpace(rawReference) ? null : rawReference.Trim();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid PaymentIntentId { get; private set; }

    public PaymentTransactionType Type { get; private set; }

    public string? ProviderTransactionId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public string? RawReference { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string? MetadataJson { get; private set; }

    public static Result<PaymentTransaction> Create(
        Guid paymentIntentId,
        PaymentTransactionType type,
        string? providerTransactionId,
        decimal amount,
        string currency,
        string status,
        string? rawReference,
        string? metadataJson,
        DateTime createdAtUtc)
    {
        if (paymentIntentId == Guid.Empty)
        {
            return Result<PaymentTransaction>.Failure(new Error(
                "payments.transaction.payment_intent.required",
                "Payment intent id is required."));
        }

        if (amount < 0m)
        {
            return Result<PaymentTransaction>.Failure(new Error(
                "payments.transaction.amount.invalid",
                "Amount cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Result<PaymentTransaction>.Failure(new Error(
                "payments.transaction.currency.invalid",
                "Currency must be a 3-letter code."));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            return Result<PaymentTransaction>.Failure(new Error(
                "payments.transaction.status.required",
                "Status is required."));
        }

        return Result<PaymentTransaction>.Success(new PaymentTransaction(
            Guid.NewGuid(),
            paymentIntentId,
            type,
            providerTransactionId,
            amount,
            currency.Trim().ToUpperInvariant(),
            status.Trim(),
            rawReference,
            metadataJson,
            createdAtUtc));
    }
}
