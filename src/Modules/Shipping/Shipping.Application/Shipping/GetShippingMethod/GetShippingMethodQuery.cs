using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.GetShippingMethod;

public sealed record GetShippingMethodQuery(Guid ShippingMethodId) : IQuery<ShippingMethodDto?>;
