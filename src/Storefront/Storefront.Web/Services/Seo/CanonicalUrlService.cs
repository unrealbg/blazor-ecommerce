using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Storefront.Web.Services.Seo;

public sealed class CanonicalUrlService(IPageMetadataService pageMetadataService) : ICanonicalUrlService
{
    public CanonicalUrls Build(
        string path,
        IReadOnlyDictionary<string, StringValues> query,
        bool hasNextPage)
    {
        var normalizedPath = NormalizePath(path);
        var currentPage = this.ParsePage(query);
        var relativePath = currentPage > 1
            ? $"{normalizedPath}?page={currentPage.ToString(CultureInfo.InvariantCulture)}"
            : normalizedPath;
        var canonicalUrl = pageMetadataService.BuildAbsoluteUrl(relativePath);
        var prevUrl = currentPage > 1
            ? pageMetadataService.BuildAbsoluteUrl(
                currentPage == 2
                    ? normalizedPath
                    : $"{normalizedPath}?page={(currentPage - 1).ToString(CultureInfo.InvariantCulture)}")
            : null;
        var nextUrl = hasNextPage
            ? pageMetadataService.BuildAbsoluteUrl(
                $"{normalizedPath}?page={(currentPage + 1).ToString(CultureInfo.InvariantCulture)}")
            : null;

        return new CanonicalUrls(canonicalUrl, prevUrl, nextUrl, currentPage, NoIndex: false);
    }

    public CanonicalUrls BuildForSearch(
        IReadOnlyDictionary<string, StringValues> query,
        bool hasNextPage)
    {
        var currentPage = this.ParsePage(query);
        var searchQuery = this.GetSearchQuery(query);

        var canonicalUrl = this.BuildSearchUrl(searchQuery, currentPage);
        var prevUrl = currentPage > 1
            ? this.BuildSearchUrl(searchQuery, currentPage - 1)
            : null;
        var nextUrl = hasNextPage
            ? this.BuildSearchUrl(searchQuery, currentPage + 1)
            : null;

        return new CanonicalUrls(canonicalUrl, prevUrl, nextUrl, currentPage, NoIndex: true);
    }

    public CanonicalUrls BuildForCategory(
        string categorySlug,
        IReadOnlyDictionary<string, StringValues> query,
        bool hasNextPage)
    {
        var normalizedSlug = NormalizeCategorySlug(categorySlug);
        var currentPage = this.ParsePage(query);
        var hasFilters = HasCategoryFilters(query);

        var canonicalUrl = this.BuildCategoryUrl(normalizedSlug, currentPage);
        var prevUrl = currentPage > 1
            ? this.BuildCategoryUrl(normalizedSlug, currentPage - 1)
            : null;
        var nextUrl = hasNextPage
            ? this.BuildCategoryUrl(normalizedSlug, currentPage + 1)
            : null;

        return new CanonicalUrls(canonicalUrl, prevUrl, nextUrl, currentPage, NoIndex: hasFilters);
    }

    private string NormalizeCategorySlug(string categorySlug)
    {
        if (string.IsNullOrWhiteSpace(categorySlug))
        {
            return "all-products";
        }

        return categorySlug.Trim().ToLowerInvariant();
    }

    private string BuildSearchUrl(string? searchQuery, int page)
    {
        var queryParameters = new List<KeyValuePair<string, string?>>(2);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("q", searchQuery));
        }

        if (page > 1)
        {
            queryParameters.Add(new KeyValuePair<string, string?>("page", page.ToString(CultureInfo.InvariantCulture)));
        }

        var relativeUrl = queryParameters.Count == 0
            ? "/search"
            : $"/search{QueryString.Create(queryParameters)}";

        return pageMetadataService.BuildAbsoluteUrl(relativeUrl);
    }

    private string BuildCategoryUrl(string categorySlug, int page)
    {
        var relativeUrl = page > 1
            ? $"/category/{categorySlug}?page={page.ToString(CultureInfo.InvariantCulture)}"
            : $"/category/{categorySlug}";

        return pageMetadataService.BuildAbsoluteUrl(relativeUrl);
    }

    private string? GetSearchQuery(IReadOnlyDictionary<string, StringValues> query)
    {
        if (!query.TryGetValue("q", out var queryValues))
        {
            return null;
        }

        var queryValue = queryValues.ToString().Trim();
        return string.IsNullOrWhiteSpace(queryValue) ? null : queryValue;
    }

    private int ParsePage(IReadOnlyDictionary<string, StringValues> query)
    {
        if (!query.TryGetValue("page", out var queryValues))
        {
            return 1;
        }

        return int.TryParse(queryValues.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPage) &&
               parsedPage > 1
            ? parsedPage
            : 1;
    }

    private bool HasCategoryFilters(IReadOnlyDictionary<string, StringValues> query)
    {
        return HasNonEmptyKey(query, "brand") ||
               HasNonEmptyKey(query, "minPrice") ||
               HasNonEmptyKey(query, "maxPrice") ||
               HasNonEmptyKey(query, "inStock") ||
               HasNonEmptyKey(query, "sort");
    }

    private bool HasNonEmptyKey(
        IReadOnlyDictionary<string, StringValues> query,
        string key)
    {
        return query.TryGetValue(key, out var values) && values.Count != 0 &&
               values.Any(value => !string.IsNullOrWhiteSpace(value));
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        return path.StartsWith("/", StringComparison.Ordinal)
            ? path
            : $"/{path}";
    }
}
