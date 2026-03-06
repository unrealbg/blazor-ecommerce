using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.ListShippingMethods;

public sealed record ListShippingMethodsQuery(bool ActiveOnly) : IQuery<IReadOnlyCollection<ShippingMethodDto>>;
