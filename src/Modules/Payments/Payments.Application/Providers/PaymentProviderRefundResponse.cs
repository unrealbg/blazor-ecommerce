using Payments.Domain.Payments;

namespace Payments.Application.Providers;

public sealed record PaymentProviderRefundResponse(
    PaymentIntentStatus Status,
    decimal RefundedAmount,
    bool IsPartial,
    string? ProviderTransactionId,
    string? RawReference,
    string? MetadataJson,
    string? FailureCode,
    string? FailureMessage);
