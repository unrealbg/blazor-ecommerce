namespace Shipping.Application.Shipping;

public sealed record ShippingMethodDto(
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
