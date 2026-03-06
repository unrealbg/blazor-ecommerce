namespace Storefront.Web.Services.Api;

public sealed record StorePaymentIntentDetails(
    Guid Id,
    Guid OrderId,
    Guid? CustomerId,
    string Provider,
    string Status,
    decimal Amount,
    string Currency,
    string? ProviderPaymentIntentId,
    string? ClientSecret,
    string? FailureCode,
    string? FailureMessage,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyCollection<StorePaymentTransaction> Transactions);
