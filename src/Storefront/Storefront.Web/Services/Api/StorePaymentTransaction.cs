namespace Storefront.Web.Services.Api;

public sealed record StorePaymentTransaction(
    Guid Id,
    string Type,
    string? ProviderTransactionId,
    decimal Amount,
    string Currency,
    string Status,
    string? RawReference,
    DateTime CreatedAtUtc,
    string? MetadataJson);
