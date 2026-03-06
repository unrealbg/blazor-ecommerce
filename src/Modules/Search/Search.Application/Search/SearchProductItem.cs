namespace Search.Application.Search;

public sealed record SearchProductItem(
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
