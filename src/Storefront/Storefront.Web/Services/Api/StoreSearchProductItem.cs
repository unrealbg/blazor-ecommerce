namespace Storefront.Web.Services.Api;

public sealed record StoreSearchProductItem(
    Guid ProductId,
    string Slug,
    string Name,
    string? Description,
    string? CategorySlug,
    string? CategoryName,
    string? Brand,
    decimal PriceAmount,
    string Currency,
    bool IsInStock,
    string? ImageUrl,
    DateTime UpdatedAtUtc);
