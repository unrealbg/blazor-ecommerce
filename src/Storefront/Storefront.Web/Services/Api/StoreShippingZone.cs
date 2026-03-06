namespace Storefront.Web.Services.Api;

public sealed record StoreShippingZone(
    Guid Id,
    string Code,
    string Name,
    IReadOnlyCollection<string> CountryCodes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
