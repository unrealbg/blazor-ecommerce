using BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Search.Application.Search;

public sealed class SearchProductsQueryHandler(
    ISearchProvider searchProvider,
    ILogger<SearchProductsQueryHandler> logger)
    : IQueryHandler<SearchProductsQuery, SearchProductsResponse>
{
    public async Task<SearchProductsResponse> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var normalized = Normalize(request);
        var result = await searchProvider.SearchAsync(normalized, cancellationToken);
        var totalPages = result.Total == 0
            ? 1
            : (int)Math.Ceiling(result.Total / (double)normalized.PageSize);

        logger.LogInformation(
            "Search performed. Query={Query} Category={CategorySlug} Brands={BrandsCount} Sort={Sort} Page={Page} Total={Total}",
            normalized.Query,
            normalized.CategorySlug,
            normalized.Brands.Count,
            normalized.Sort,
            normalized.Page,
            result.Total);

        if (result.Total == 0)
        {
            logger.LogInformation(
                "Search returned zero results. Query={Query} Category={CategorySlug}",
                normalized.Query,
                normalized.CategorySlug);
        }

        if (normalized.Brands.Count != 0 || normalized.MinPrice is not null || normalized.MaxPrice is not null || normalized.InStock is not null)
        {
            logger.LogInformation(
                "Search filters applied. Query={Query} Category={CategorySlug} Brands={Brands} MinPrice={MinPrice} MaxPrice={MaxPrice} InStock={InStock}",
                normalized.Query,
                normalized.CategorySlug,
                string.Join(',', normalized.Brands),
                normalized.MinPrice,
                normalized.MaxPrice,
                normalized.InStock);
        }

        return new SearchProductsResponse(
            result.Items,
            result.Total,
            normalized.Page,
            normalized.PageSize,
            totalPages,
            result.Facets,
            new SearchAppliedFilters(
                normalized.Query,
                normalized.CategorySlug,
                normalized.Brands,
                normalized.MinPrice,
                normalized.MaxPrice,
                normalized.InStock,
                normalized.Sort,
                normalized.Page,
                normalized.PageSize));
    }

    private static ProductSearchRequest Normalize(SearchProductsQuery request)
    {
        var normalizedQuery = string.IsNullOrWhiteSpace(request.Query)
            ? null
            : request.Query.Trim();

        var normalizedCategorySlug = string.IsNullOrWhiteSpace(request.CategorySlug)
            ? null
            : request.CategorySlug.Trim().ToLowerInvariant();

        var normalizedBrands = (request.Brands ?? [])
            .Where(brand => !string.IsNullOrWhiteSpace(brand))
            .Select(brand => brand.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(brand => brand, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var normalizedMin = request.MinPrice is <= 0 ? null : request.MinPrice;
        var normalizedMax = request.MaxPrice is <= 0 ? null : request.MaxPrice;

        if (normalizedMin is not null && normalizedMax is not null && normalizedMin > normalizedMax)
        {
            (normalizedMin, normalizedMax) = (normalizedMax, normalizedMin);
        }

        var normalizedPage = request.Page <= 0 ? 1 : request.Page;
        var normalizedPageSize = request.PageSize <= 0
            ? 24
            : Math.Min(request.PageSize, 100);

        var normalizedSort = SearchSortOptions.Normalize(request.Sort, normalizedQuery is not null);

        return new ProductSearchRequest(
            normalizedQuery,
            normalizedCategorySlug,
            normalizedBrands,
            normalizedMin,
            normalizedMax,
            request.InStock,
            normalizedSort,
            normalizedPage,
            normalizedPageSize);
    }
}
