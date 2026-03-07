namespace Pricing.Application.Pricing;

public sealed record VariantPriceDto(
    Guid Id,
    Guid PriceListId,
    Guid VariantId,
    decimal BasePriceAmount,
    decimal? CompareAtPriceAmount,
    string Currency,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
