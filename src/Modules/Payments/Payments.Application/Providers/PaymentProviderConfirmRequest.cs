namespace Payments.Application.Providers;

public sealed record PaymentProviderConfirmRequest(
    Guid PaymentIntentId,
    string ProviderPaymentIntentId,
    decimal Amount,
    string Currency,
    IReadOnlyDictionary<string, string> Metadata);
