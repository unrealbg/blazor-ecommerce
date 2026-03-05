namespace Storefront.Web.Services.Api;

public sealed record StoreProduct(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string Currency,
    decimal Amount,
    bool IsActive);
