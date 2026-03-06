using Payments.Domain.Payments;

namespace Payments.Application.Providers;

public sealed record PaymentProviderWebhookResult(
    string ExternalEventId,
    string EventType,
    string ProviderPaymentIntentId,
    PaymentIntentStatus Status,
    decimal? Amount,
    string? Currency,
    string? ProviderTransactionId,
    string? RawReference,
    string? MetadataJson,
    string? FailureCode,
    string? FailureMessage,
    bool IsPartialRefund);
