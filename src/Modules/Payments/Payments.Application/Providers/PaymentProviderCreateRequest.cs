namespace Payments.Application.Providers;

public sealed record PaymentProviderCreateRequest(
    Guid OrderId,
    Guid PaymentIntentId,
    decimal Amount,
    string Currency,
    string? CustomerEmail,
    string? ReturnUrl,
    string? CallbackUrl,
    IReadOnlyDictionary<string, string> Metadata);
