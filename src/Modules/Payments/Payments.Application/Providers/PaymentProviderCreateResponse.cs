using Payments.Domain.Payments;

namespace Payments.Application.Providers;

public sealed record PaymentProviderCreateResponse(
    string ProviderPaymentIntentId,
    string? ClientSecret,
    PaymentIntentStatus Status,
    bool RequiresAction,
    string? RedirectUrl,
    string? FailureCode,
    string? FailureMessage,
    string? ProviderTransactionId,
    string? RawReference,
    string? MetadataJson);
