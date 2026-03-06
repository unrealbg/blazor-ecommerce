namespace BuildingBlocks.Application.Contracts;

public sealed record ShippingQuoteSelection(
    Guid ShippingMethodId,
    string ShippingMethodCode,
    string ShippingMethodName,
    decimal PriceAmount,
    string Currency,
    int? EstimatedMinDays,
    int? EstimatedMaxDays,
    bool IsFreeShipping);
