namespace Shipping.Application.Shipping;

public sealed record ShippingQuoteResponse(IReadOnlyCollection<ShippingQuoteMethodDto> Methods);
