namespace Catalog.Application.Products;

public sealed record ProductDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string? Brand,
    string? Sku,
    string? ImageUrl,
    bool IsInStock,
    string? CategorySlug,
    string? CategoryName,
    string Currency,
    decimal Amount,
    bool IsActive,
    bool IsTracked = false,
    bool AllowBackorder = false,
    int? AvailableQuantity = null);
