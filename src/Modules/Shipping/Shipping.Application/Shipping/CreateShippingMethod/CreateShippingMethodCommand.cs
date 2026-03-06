using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.CreateShippingMethod;

public sealed record CreateShippingMethodCommand(
    string Code,
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
    int Priority) : ICommand<Guid>;
