using Payments.Domain.Payments;

namespace Payments.Application.Providers;

public sealed record PaymentProviderCancelResponse(
    PaymentIntentStatus Status,
    string? FailureCode,
    string? FailureMessage,
    string? ProviderTransactionId,
    string? RawReference,
    string? MetadataJson);
