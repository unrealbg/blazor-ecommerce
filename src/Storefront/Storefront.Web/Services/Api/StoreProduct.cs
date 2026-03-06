namespace Storefront.Web.Services.Api;

public sealed record StoreProduct(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string? Brand,
    string? Sku,
    string? ImageUrl,
    bool IsInStock,
    bool IsTracked,
    bool AllowBackorder,
    int? AvailableQuantity,
    string? CategorySlug,
    string? CategoryName,
    string Currency,
    decimal Amount,
    bool IsActive);
