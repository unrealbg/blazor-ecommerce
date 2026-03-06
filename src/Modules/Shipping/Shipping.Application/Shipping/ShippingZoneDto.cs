namespace Shipping.Application.Shipping;

public sealed record ShippingZoneDto(
    Guid Id,
    string Code,
    string Name,
    IReadOnlyCollection<string> CountryCodes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
