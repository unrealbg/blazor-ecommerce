namespace Storefront.Web.Services.Api;

public sealed record StoreShippingMethod(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Provider,
    string Type,
    decimal BasePriceAmount,
    string Currency,
    bool IsActive,
    bool SupportsTracking,
    bool SupportsPickupPoint,
    int? EstimatedMinDays,
    int? EstimatedMaxDays,
    int Priority,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
