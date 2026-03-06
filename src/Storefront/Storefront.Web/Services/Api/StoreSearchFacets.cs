namespace Storefront.Web.Services.Api;

public sealed record StoreSearchFacets(
    IReadOnlyCollection<StoreSearchBrandFacetItem> Brands,
    IReadOnlyCollection<StoreSearchCategoryFacetItem> Categories,
    int InStockCount,
    StoreSearchPriceSummary Price);
