namespace Storefront.Web.Services.Api;

public sealed record StorePriceList(
    Guid Id,
    string Name,
    string Code,
    string Currency,
    bool IsDefault,
    bool IsActive,
    int Priority,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
