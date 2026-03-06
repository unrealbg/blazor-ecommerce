namespace Storefront.Web.Services.Api;

public sealed record StoreSearchProductsRequest(
    string? Query,
    string? CategorySlug,
    IReadOnlyCollection<string> Brands,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? InStock,
    string? Sort,
    int Page = 1,
    int PageSize = 24);
