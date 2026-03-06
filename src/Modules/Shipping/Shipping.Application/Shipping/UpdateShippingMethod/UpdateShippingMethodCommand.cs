using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.UpdateShippingMethod;

public sealed record UpdateShippingMethodCommand(
    Guid ShippingMethodId,
    string Name,
    string? Description,
    string Provider,
    string Type,
    decimal BasePriceAmount,
    string Currency,
    bool SupportsTracking,
    bool SupportsPickupPoint,
    int? EstimatedMinDays,
    int? EstimatedMaxDays,
    int Priority,
    bool IsActive) : ICommand<bool>;
