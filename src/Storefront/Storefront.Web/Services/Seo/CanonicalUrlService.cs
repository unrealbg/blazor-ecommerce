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
        var currentPage = ParsePage(query);
        var searchQuery = GetSearchQuery(normalizedPath, query);

        var canonicalUrl = BuildPageUrl(normalizedPath, searchQuery, currentPage);
        var prevUrl = currentPage > 1
            ? BuildPageUrl(normalizedPath, searchQuery, currentPage - 1)
            : null;
        var nextUrl = hasNextPage
            ? BuildPageUrl(normalizedPath, searchQuery, currentPage + 1)
            : null;

        return new CanonicalUrls(canonicalUrl, prevUrl, nextUrl, currentPage);
    }

    private string BuildPageUrl(string path, string? searchQuery, int page)
    {
        var queryParameters = new List<KeyValuePair<string, string?>>(2);

        if (!string.IsNullOrWhiteSpace(searchQuery) &&
            string.Equals(path, "/search", StringComparison.OrdinalIgnoreCase))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("q", searchQuery));
        }

        if (page > 1)
        {
            queryParameters.Add(new KeyValuePair<string, string?>("page", page.ToString(CultureInfo.InvariantCulture)));
        }

        var relativeUrl = queryParameters.Count == 0
            ? path
            : $"{path}{QueryString.Create(queryParameters)}";

        return pageMetadataService.BuildAbsoluteUrl(relativeUrl);
    }

    private string? GetSearchQuery(string path, IReadOnlyDictionary<string, StringValues> query)
    {
        if (!string.Equals(path, "/search", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

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

        return int.TryParse(queryValues.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPage) &&
               parsedPage > 1
            ? parsedPage
            : 1;
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
