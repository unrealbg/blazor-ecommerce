namespace Search.Application.Search;

public sealed record SearchFacets(
    IReadOnlyCollection<SearchBrandFacetItem> Brands,
    IReadOnlyCollection<SearchCategoryFacetItem> Categories,
    int InStockCount,
    SearchPriceSummary Price);
