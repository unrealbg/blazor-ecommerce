namespace Pricing.Application.Pricing;

public sealed record CreateVariantPriceRequest(
    Guid PriceListId,
    Guid VariantId,
    decimal BasePriceAmount,
    decimal? CompareAtPriceAmount,
    string Currency,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc);
