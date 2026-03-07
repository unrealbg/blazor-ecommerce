namespace Pricing.Application.Pricing;

public sealed record CreatePriceListRequest(
    string Name,
    string Code,
    string Currency,
    bool IsDefault,
    bool IsActive,
    int Priority);
