namespace Storefront.Web.Services.Seo;

public sealed record ProductSeoModel(
    string Name,
    string? Description,
    string Currency,
    decimal PriceAmount,
    bool IsInStock,
    string? Sku,
    string? Brand,
    IReadOnlyCollection<string> ImageUrls,
    decimal? AverageRating,
    int ApprovedReviewCount);
