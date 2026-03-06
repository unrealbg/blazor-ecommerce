namespace Storefront.Web.Services.Api;

public sealed record StoreSearchProductsResponse(
    IReadOnlyCollection<StoreSearchProductItem> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages,
    StoreSearchFacets Facets,
    StoreSearchAppliedFilters AppliedFilters);
