using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.ListShippingRateRules;

public sealed record ListShippingRateRulesQuery(bool ActiveOnly) : IQuery<IReadOnlyCollection<ShippingRateRuleDto>>;
