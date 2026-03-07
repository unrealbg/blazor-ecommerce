namespace BuildingBlocks.Application.Contracts;

public sealed record VariantPricingSnapshot(
    Guid VariantId,
    string Currency,
    decimal BasePriceAmount,
    decimal? CompareAtPriceAmount,
    decimal EffectivePriceAmount,
    bool IsDiscounted,
    IReadOnlyCollection<PricingDiscountApplication> AppliedDiscounts);
