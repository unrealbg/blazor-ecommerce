namespace Shipping.Application.Shipping;

public sealed record ShippingQuoteMethodDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int? EstimatedMinDays,
    int? EstimatedMaxDays,
    bool IsFreeShipping);
