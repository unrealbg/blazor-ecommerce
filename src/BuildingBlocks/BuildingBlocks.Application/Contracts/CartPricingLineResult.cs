namespace BuildingBlocks.Application.Contracts;

public sealed record CartPricingLineResult(
    Guid ProductId,
    Guid VariantId,
    string Currency,
    decimal BaseUnitPriceAmount,
    decimal? CompareAtUnitPriceAmount,
    decimal FinalUnitPriceAmount,
    decimal LineTotalAmount,
    decimal DiscountTotalAmount,
    IReadOnlyCollection<PricingDiscountApplication> AppliedDiscounts);
