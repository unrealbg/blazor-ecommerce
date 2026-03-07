namespace Pricing.Application.Pricing;

public sealed record PriceListDto(
    Guid Id,
    string Name,
    string Code,
    string Currency,
    bool IsDefault,
    bool IsActive,
    int Priority,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
