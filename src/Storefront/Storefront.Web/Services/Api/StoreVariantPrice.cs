namespace Storefront.Web.Services.Api;

public sealed record StoreVariantPrice(
    Guid Id,
    Guid PriceListId,
    Guid VariantId,
    decimal BasePriceAmount,
    decimal? CompareAtPriceAmount,
    string Currency,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
