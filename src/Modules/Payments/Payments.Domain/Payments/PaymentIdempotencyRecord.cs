using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Payments.Domain.Payments;

public sealed class PaymentIdempotencyRecord : Entity<Guid>
{
    private PaymentIdempotencyRecord()
    {
    }

    private PaymentIdempotencyRecord(
        Guid id,
        string operation,
        string idempotencyKey,
        Guid paymentIntentId,
        DateTime createdAtUtc)
    {
        Id = id;
        Operation = operation;
        IdempotencyKey = idempotencyKey;
        PaymentIntentId = paymentIntentId;
        CreatedAtUtc = createdAtUtc;
    }

    public string Operation { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public Guid PaymentIntentId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Result<PaymentIdempotencyRecord> Create(
        string operation,
        string idempotencyKey,
        Guid paymentIntentId,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(operation))
        {
            return Result<PaymentIdempotencyRecord>.Failure(new Error(
                "payments.idempotency.operation.required",
                "Idempotency operation is required."));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result<PaymentIdempotencyRecord>.Failure(new Error(
                "payments.idempotency.key.required",
                "Idempotency key is required."));
        }

        if (paymentIntentId == Guid.Empty)
        {
            return Result<PaymentIdempotencyRecord>.Failure(new Error(
                "payments.idempotency.payment_intent.required",
                "Payment intent id is required."));
        }

        return Result<PaymentIdempotencyRecord>.Success(new PaymentIdempotencyRecord(
            Guid.NewGuid(),
            operation.Trim(),
            idempotencyKey.Trim(),
            paymentIntentId,
            createdAtUtc));
    }
}
