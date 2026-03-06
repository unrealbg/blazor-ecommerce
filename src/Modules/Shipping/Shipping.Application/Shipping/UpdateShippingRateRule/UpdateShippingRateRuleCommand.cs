using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.UpdateShippingRateRule;

public sealed record UpdateShippingRateRuleCommand(
    Guid ShippingRateRuleId,
    decimal? MinOrderAmount,
    decimal? MaxOrderAmount,
    decimal? MinWeightKg,
    decimal? MaxWeightKg,
    decimal PriceAmount,
    decimal? FreeShippingThresholdAmount,
    string Currency,
    bool IsActive) : ICommand<bool>;
