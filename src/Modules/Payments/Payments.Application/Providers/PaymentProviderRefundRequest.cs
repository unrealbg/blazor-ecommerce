namespace Payments.Application.Providers;

public sealed record PaymentProviderRefundRequest(
    Guid PaymentIntentId,
    string ProviderPaymentIntentId,
    decimal OriginalAmount,
    decimal? RefundAmount,
    string Currency,
    string? Reason,
    IReadOnlyDictionary<string, string> Metadata);
