namespace Payments.Application.Providers;

public sealed record PaymentProviderCancelRequest(
    Guid PaymentIntentId,
    string ProviderPaymentIntentId,
    string? Reason,
    IReadOnlyDictionary<string, string> Metadata);
