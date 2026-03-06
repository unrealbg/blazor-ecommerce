namespace Search.Application.Search;

public sealed record SearchAppliedFilters(
    string? Query,
    string? CategorySlug,
    IReadOnlyCollection<string> Brands,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? InStock,
    string Sort,
    int Page,
    int PageSize);
