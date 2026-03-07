namespace BuildingBlocks.Application.Contracts;

public sealed record ShippingPriceSelection(
    string ShippingMethodCode,
    string Currency,
    decimal PriceAmount);
