namespace Storefront.Web.Services.Api;

public sealed record StoreVariantPriceRequest(
    Guid PriceListId,
    Guid VariantId,
    decimal BasePriceAmount,
    decimal? CompareAtPriceAmount,
    string Currency,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc);
