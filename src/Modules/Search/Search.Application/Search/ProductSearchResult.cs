namespace Search.Application.Search;

public sealed record ProductSearchResult(
    IReadOnlyCollection<SearchProductItem> Items,
    int Total,
    SearchFacets Facets);
