namespace Search.Application.Search;

public sealed record SearchProductsResponse(
    IReadOnlyCollection<SearchProductItem> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages,
    SearchFacets Facets,
    SearchAppliedFilters AppliedFilters);
