namespace Storefront.Web.Services.Api;

public sealed record StoreSearchAppliedFilters(
    string? Query,
    string? CategorySlug,
    IReadOnlyCollection<string> Brands,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? InStock,
    string Sort,
    int Page,
    int PageSize);
