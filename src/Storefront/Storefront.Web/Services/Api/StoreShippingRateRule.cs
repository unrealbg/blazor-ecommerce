namespace Storefront.Web.Services.Api;

public sealed record StoreShippingRateRule(
    Guid Id,
    Guid ShippingMethodId,
    Guid ShippingZoneId,
    decimal? MinOrderAmount,
    decimal? MaxOrderAmount,
    decimal? MinWeightKg,
    decimal? MaxWeightKg,
    decimal PriceAmount,
    decimal? FreeShippingThresholdAmount,
    string Currency,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
