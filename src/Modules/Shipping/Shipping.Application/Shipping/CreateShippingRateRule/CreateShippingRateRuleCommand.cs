using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.CreateShippingRateRule;

public sealed record CreateShippingRateRuleCommand(
    Guid ShippingMethodId,
    Guid ShippingZoneId,
    decimal? MinOrderAmount,
    decimal? MaxOrderAmount,
    decimal? MinWeightKg,
    decimal? MaxWeightKg,
    decimal PriceAmount,
    decimal? FreeShippingThresholdAmount,
    string Currency) : ICommand<Guid>;
