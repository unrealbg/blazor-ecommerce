namespace Pricing.Application.Pricing;

public sealed record UpdateVariantPriceRequest(
    decimal BasePriceAmount,
    decimal? CompareAtPriceAmount,
    string Currency,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc);
