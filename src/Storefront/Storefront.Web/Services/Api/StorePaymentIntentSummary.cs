namespace Storefront.Web.Services.Api;

public sealed record StorePaymentIntentSummary(
    Guid Id,
    Guid OrderId,
    string Provider,
    string Status,
    decimal Amount,
    string Currency,
    string? ProviderPaymentIntentId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc);
