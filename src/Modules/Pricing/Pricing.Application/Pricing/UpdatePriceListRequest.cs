namespace Pricing.Application.Pricing;

public sealed record UpdatePriceListRequest(
    string Name,
    string Currency,
    bool IsDefault,
    bool IsActive,
    int Priority);
