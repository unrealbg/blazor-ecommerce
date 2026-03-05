using Microsoft.Extensions.Options;

namespace Storefront.Web.Services.Seo;

public sealed class PageMetadataService(IOptions<SeoOptions> options) : IPageMetadataService
{
    private readonly string baseUrl = NormalizeBaseUrl(options.Value.SiteBaseUrl);

    public PageMetadata ForHome()
    {
        return new PageMetadata(
            "Storefront Home",
            "Discover featured products and shop online in EUR.",
            BuildAbsoluteUrl("/"));
    }

    public PageMetadata ForCategory(string categoryName, string categorySlug)
    {
        return new PageMetadata(
            $"Category: {categoryName}",
            $"Browse products in the {categoryName} category.",
            BuildAbsoluteUrl($"/category/{categorySlug}"));
    }

    public PageMetadata ForProduct(string productName, string? description, string productSlug)
    {
        return new PageMetadata(
            $"{productName} - Buy Online",
            BuildProductDescription(description),
            BuildAbsoluteUrl($"/product/{productSlug}"));
    }

    public PageMetadata ForSearch(string query)
    {
        var normalizedQuery = query.Trim();

        return new PageMetadata(
            $"Search: {normalizedQuery}",
            $"Search results for '{normalizedQuery}' products.",
            BuildAbsoluteUrl($"/search?q={Uri.EscapeDataString(normalizedQuery)}"));
    }

    public string ResolveCanonicalUrl(string? preferredCanonicalUrl, string fallbackRelativePath)
    {
        return this.ResolveAbsoluteOptionalUrl(preferredCanonicalUrl) ??
               this.BuildAbsoluteUrl(fallbackRelativePath);
    }

    public string? ResolveAbsoluteOptionalUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out _))
        {
            return normalized;
        }

        return this.BuildAbsoluteUrl(normalized);
    }

    public string BuildAbsoluteUrl(string relativePathAndQuery)
    {
        if (string.IsNullOrWhiteSpace(relativePathAndQuery) || relativePathAndQuery == "/")
        {
            return $"{baseUrl}/";
        }

        var normalizedPath = relativePathAndQuery.StartsWith('/')
            ? relativePathAndQuery
            : $"/{relativePathAndQuery}";

        return $"{baseUrl}{normalizedPath}";
    }

    private static string BuildProductDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return "Buy this product online with fast and secure checkout.";
        }

        var normalized = description.Trim();
        return normalized.Length <= 160 ? normalized : normalized[..160];
    }

    private static string NormalizeBaseUrl(string siteBaseUrl)
    {
        return string.IsNullOrWhiteSpace(siteBaseUrl)
            ? "http://localhost:5100"
            : siteBaseUrl.TrimEnd('/');
    }
}
