namespace BuildingBlocks.Application.Contracts;

public sealed record OrderPricingLineSnapshot(
    Guid ProductId,
    Guid VariantId,
    string? Sku,
    decimal BaseUnitPriceAmount,
    decimal FinalUnitPriceAmount,
    decimal DiscountTotalAmount,
    int Quantity,
    IReadOnlyCollection<PricingDiscountApplication> AppliedDiscounts);
