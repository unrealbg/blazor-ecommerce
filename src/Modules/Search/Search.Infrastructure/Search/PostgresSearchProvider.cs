using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Search.Application.Search;
using Search.Domain.Documents;
using Search.Infrastructure.Persistence;

namespace Search.Infrastructure.Search;

internal sealed class PostgresSearchProvider(
    SearchDbContext dbContext,
    ILogger<PostgresSearchProvider> logger)
    : ISearchProvider
{
    private const string SearchConfiguration = "simple";

    public async Task<ProductSearchResult> SearchAsync(
        ProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        var baseQuery = dbContext.ProductSearchDocuments
            .AsNoTracking()
            .Where(document => document.IsActive);

        var queryFiltered = ApplyQuery(baseQuery, request.Query);
        var stockFiltered = ApplyStock(queryFiltered, request.InStock);
        var categoryFiltered = ApplyCategory(stockFiltered, request.CategorySlug);
        var priceScopedForFacets = ApplyBrands(categoryFiltered, request.Brands);

        var priceSummary = await BuildPriceSummaryAsync(priceScopedForFacets, cancellationToken);
        var inStockCount = await categoryFiltered.CountAsync(document => document.IsInStock, cancellationToken);

        var brandFacetScope = ApplyPriceRange(categoryFiltered, request.MinPrice, request.MaxPrice);
        var brandFacets = await BuildBrandFacetsAsync(brandFacetScope, request.Brands, cancellationToken);

        var categoryFacetScope = ApplyBrands(stockFiltered, request.Brands);
        categoryFacetScope = ApplyPriceRange(categoryFacetScope, request.MinPrice, request.MaxPrice);
        var categoryFacets = await BuildCategoryFacetsAsync(
            categoryFacetScope,
            request.CategorySlug,
            cancellationToken);

        var filtered = ApplyPriceRange(priceScopedForFacets, request.MinPrice, request.MaxPrice);
        var total = await filtered.CountAsync(cancellationToken);

        var sorted = ApplySort(filtered, request);
        var items = await sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(document => new SearchProductItem(
                document.ProductId,
                document.Slug,
                document.Name,
                document.DescriptionText,
                document.CategorySlug,
                document.CategoryName,
                document.Brand,
                document.PriceAmount,
                document.Currency,
                document.IsInStock,
                document.ImageUrl,
                document.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        var elapsedMilliseconds = (DateTime.UtcNow - startedAt).TotalMilliseconds;
        if (elapsedMilliseconds > 250)
        {
            logger.LogWarning(
                "Search query took {ElapsedMs}ms. Query={Query} Category={CategorySlug} Page={Page}",
                elapsedMilliseconds,
                request.Query,
                request.CategorySlug,
                request.Page);
        }

        var facets = new SearchFacets(brandFacets, categoryFacets, inStockCount, priceSummary);
        return new ProductSearchResult(items, total, facets);
    }

    public async Task<IReadOnlyCollection<SearchSuggestionItem>> SuggestAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim();
        if (normalizedQuery.Length < 2)
        {
            return [];
        }

        if (!this.IsNpgsqlProvider())
        {
            return await this.SuggestFallbackAsync(normalizedQuery, limit, cancellationToken);
        }

        var normalizedLower = normalizedQuery.ToLowerInvariant();
        var containsPattern = $"%{normalizedLower}%";
        var prefixPattern = $"{normalizedLower}%";

        var suggestions = await dbContext.ProductSearchDocuments
            .AsNoTracking()
            .Where(document => document.IsActive)
            .Where(document =>
                EF.Functions.ILike(document.Name, containsPattern) ||
                (document.Brand != null && EF.Functions.ILike(document.Brand, containsPattern)) ||
                EF.Functions.TrigramsAreSimilar(document.Name, normalizedLower))
            .OrderByDescending(document => document.NormalizedName == normalizedLower)
            .ThenByDescending(document => EF.Functions.ILike(document.Name, prefixPattern))
            .ThenByDescending(document => EF.Functions.TrigramsSimilarity(document.Name, normalizedLower))
            .ThenByDescending(document => document.IsInStock)
            .ThenBy(document => document.Name)
            .Take(limit)
            .Select(document => new SearchSuggestionItem(
                document.Name,
                document.Slug,
                document.ImageUrl,
                document.PriceAmount,
                document.Currency))
            .ToArrayAsync(cancellationToken);

        return suggestions;
    }

    private async Task<IReadOnlyCollection<SearchSuggestionItem>> SuggestFallbackAsync(
        string normalizedQuery,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalizedLower = normalizedQuery.ToLowerInvariant();

        var candidates = await dbContext.ProductSearchDocuments
            .AsNoTracking()
            .Where(document => document.IsActive)
            .Where(document =>
                document.NormalizedName.Contains(normalizedLower) ||
                (document.Brand != null && document.Brand.ToLower().Contains(normalizedLower)))
            .Select(document => new
            {
                document.Name,
                document.Slug,
                document.ImageUrl,
                document.PriceAmount,
                document.Currency,
                document.IsInStock,
            })
            .Take(120)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(candidate => new
            {
                Score = CalculateSuggestionScore(candidate.Name, normalizedLower, candidate.IsInStock),
                Item = new SearchSuggestionItem(
                    candidate.Name,
                    candidate.Slug,
                    candidate.ImageUrl,
                    candidate.PriceAmount,
                    candidate.Currency),
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(item => item.Item)
            .ToArray();
    }

    private IQueryable<ProductSearchDocument> ApplyQuery(
        IQueryable<ProductSearchDocument> source,
        string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return source;
        }

        var normalized = query.Trim().ToLowerInvariant();

        if (!this.IsNpgsqlProvider())
        {
            return source.Where(document =>
                document.NormalizedName.Contains(normalized) ||
                (document.DescriptionText != null && document.DescriptionText.ToLower().Contains(normalized)) ||
                (document.Brand != null && document.Brand.ToLower().Contains(normalized)) ||
                (document.CategoryName != null && document.CategoryName.ToLower().Contains(normalized)));
        }

        var containsPattern = $"%{normalized}%";

        return source.Where(document =>
            EF.Functions.ToTsVector(SearchConfiguration, document.Name + " " + (document.DescriptionText ?? string.Empty) + " " + (document.Brand ?? string.Empty) + " " + (document.CategoryName ?? string.Empty) + " " + (document.SearchText ?? string.Empty))
                .Matches(EF.Functions.WebSearchToTsQuery(SearchConfiguration, normalized)) ||
            EF.Functions.ILike(document.Name, containsPattern) ||
            (document.Brand != null && EF.Functions.ILike(document.Brand, containsPattern)) ||
            EF.Functions.TrigramsAreSimilar(document.Name, normalized));
    }

    private IQueryable<ProductSearchDocument> ApplyCategory(
        IQueryable<ProductSearchDocument> source,
        string? categorySlug)
    {
        if (string.IsNullOrWhiteSpace(categorySlug))
        {
            return source;
        }

        var normalized = categorySlug.Trim().ToLowerInvariant();
        return source.Where(document => document.CategorySlug == normalized);
    }

    private IQueryable<ProductSearchDocument> ApplyBrands(
        IQueryable<ProductSearchDocument> source,
        IReadOnlyCollection<string> brands)
    {
        if (brands.Count == 0)
        {
            return source;
        }

        var normalizedSet = brands
            .Where(brand => !string.IsNullOrWhiteSpace(brand))
            .Select(brand => brand.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedSet.Length == 0)
        {
            return source;
        }

        return source.Where(document =>
            document.Brand != null &&
            normalizedSet.Contains(document.Brand.ToLower()));
    }

    private IQueryable<ProductSearchDocument> ApplyPriceRange(
        IQueryable<ProductSearchDocument> source,
        decimal? minPrice,
        decimal? maxPrice)
    {
        if (minPrice is not null)
        {
            source = source.Where(document => document.PriceAmount >= minPrice.Value);
        }

        if (maxPrice is not null)
        {
            source = source.Where(document => document.PriceAmount <= maxPrice.Value);
        }

        return source;
    }

    private IQueryable<ProductSearchDocument> ApplyStock(
        IQueryable<ProductSearchDocument> source,
        bool? inStock)
    {
        return inStock switch
        {
            true => source.Where(document => document.IsInStock),
            false => source.Where(document => !document.IsInStock),
            _ => source,
        };
    }

    private IQueryable<ProductSearchDocument> ApplySort(
        IQueryable<ProductSearchDocument> source,
        ProductSearchRequest request)
    {
        return request.Sort switch
        {
            SearchSortOptions.Newest => source
                .OrderByDescending(document => document.CreatedAtUtc)
                .ThenByDescending(document => document.UpdatedAtUtc),
            SearchSortOptions.PriceAscending => source
                .OrderBy(document => document.PriceAmount)
                .ThenBy(document => document.Name),
            SearchSortOptions.PriceDescending => source
                .OrderByDescending(document => document.PriceAmount)
                .ThenBy(document => document.Name),
            SearchSortOptions.NameAscending => source
                .OrderBy(document => document.Name),
            SearchSortOptions.Popular => source
                .OrderByDescending(document => document.PopularityScore ?? 0m)
                .ThenByDescending(document => document.CreatedAtUtc),
            _ => ApplyRelevanceSort(source, request.Query),
        };
    }

    private IQueryable<ProductSearchDocument> ApplyRelevanceSort(
        IQueryable<ProductSearchDocument> source,
        string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return source
                .OrderByDescending(document => document.PopularityScore ?? 0m)
                .ThenByDescending(document => document.CreatedAtUtc);
        }

        var normalized = query.Trim().ToLowerInvariant();

        if (!this.IsNpgsqlProvider())
        {
            return source
                .OrderByDescending(document => document.NormalizedName == normalized)
                .ThenByDescending(document => document.NormalizedName.StartsWith(normalized))
                .ThenByDescending(document => document.IsInStock)
                .ThenByDescending(document => document.PopularityScore ?? 0m)
                .ThenBy(document => document.Name);
        }

        var prefixPattern = $"{normalized}%";

        return source
            .OrderByDescending(document => document.NormalizedName == normalized)
            .ThenByDescending(document => EF.Functions.ILike(document.Name, prefixPattern))
            .ThenByDescending(document => EF.Functions.ToTsVector(SearchConfiguration, document.Name + " " + (document.DescriptionText ?? string.Empty) + " " + (document.Brand ?? string.Empty) + " " + (document.CategoryName ?? string.Empty) + " " + (document.SearchText ?? string.Empty))
                .Rank(EF.Functions.WebSearchToTsQuery(SearchConfiguration, normalized)))
            .ThenByDescending(document => EF.Functions.TrigramsSimilarity(document.Name, normalized))
            .ThenByDescending(document => document.IsInStock)
            .ThenByDescending(document => document.PopularityScore ?? 0m)
            .ThenBy(document => document.Name);
    }

    private bool IsNpgsqlProvider()
    {
        var providerName = dbContext.Database.ProviderName;
        return providerName is not null &&
               providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyCollection<SearchBrandFacetItem>> BuildBrandFacetsAsync(
        IQueryable<ProductSearchDocument> source,
        IReadOnlyCollection<string> selectedBrands,
        CancellationToken cancellationToken)
    {
        var selectedSet = selectedBrands
            .Select(brand => brand.Trim())
            .Where(brand => brand.Length != 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var grouped = await source
            .Where(document => document.Brand != null && document.Brand != string.Empty)
            .GroupBy(document => document.Brand!)
            .Select(group => new
            {
                Value = group.Key,
                Count = group.Count(),
            })
            .OrderBy(item => item.Value)
            .ToListAsync(cancellationToken);

        return grouped
            .Select(item => new SearchBrandFacetItem(
                item.Value,
                item.Count,
                selectedSet.Contains(item.Value)))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<SearchCategoryFacetItem>> BuildCategoryFacetsAsync(
        IQueryable<ProductSearchDocument> source,
        string? selectedCategorySlug,
        CancellationToken cancellationToken)
    {
        var normalizedSelectedSlug = string.IsNullOrWhiteSpace(selectedCategorySlug)
            ? null
            : selectedCategorySlug.Trim().ToLowerInvariant();

        var grouped = await source
            .Where(document => document.CategorySlug != null && document.CategoryName != null)
            .GroupBy(document => new { document.CategorySlug, document.CategoryName })
            .Select(group => new
            {
                group.Key.CategorySlug,
                group.Key.CategoryName,
                Count = group.Count(),
            })
            .OrderBy(item => item.CategoryName)
            .ToListAsync(cancellationToken);

        return grouped
            .Select(item =>
            {
                var selected = normalizedSelectedSlug != null &&
                               item.CategorySlug == normalizedSelectedSlug;

                return new SearchCategoryFacetItem(
                    item.CategorySlug!,
                    item.CategoryName!,
                    item.Count,
                    selected);
            })
            .ToArray();
    }

    private async Task<SearchPriceSummary> BuildPriceSummaryAsync(
        IQueryable<ProductSearchDocument> source,
        CancellationToken cancellationToken)
    {
        var min = await source
            .Select(document => (decimal?)document.PriceAmount)
            .MinAsync(cancellationToken);
        var max = await source
            .Select(document => (decimal?)document.PriceAmount)
            .MaxAsync(cancellationToken);

        return new SearchPriceSummary(min, max);
    }

    private int CalculateSuggestionScore(string name, string query, bool isInStock)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        var score = 0;

        if (normalizedName == query)
        {
            score += 1000;
        }

        if (normalizedName.StartsWith(query, StringComparison.Ordinal))
        {
            score += 700;
        }

        if (normalizedName.Contains(query, StringComparison.Ordinal))
        {
            score += 400;
        }

        if (isInStock)
        {
            score += 50;
        }

        score -= Math.Min(normalizedName.Length, 120);
        return score;
    }
}
