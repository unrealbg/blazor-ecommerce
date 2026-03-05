using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Storefront.Web.Services.Api;
using Storefront.Web.Services.Content;

namespace Storefront.Web.Services.Seo;

public sealed class SitemapService(
    IStoreApiClient storeApiClient,
    IContentClient contentClient,
    IOptions<SiteOptions> siteOptions)
    : ISitemapService
{
    private readonly string baseUrl = NormalizeBaseUrl(siteOptions.Value.BaseUrl);

    public async Task<string> BuildXmlAsync(CancellationToken cancellationToken)
    {
        var products = await storeApiClient.GetProductsAsync(cancellationToken);

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urlSet = new XElement(ns + "urlset");

        urlSet.Add(CreateUrlElement(ns, $"{baseUrl}/"));
        urlSet.Add(CreateUrlElement(ns, $"{baseUrl}/blog"));

        var activeProducts = products
            .Where(product => product.IsActive)
            .Where(product => string.Equals(product.Currency, "EUR", StringComparison.Ordinal))
            .ToList();

        var categorySlugs = activeProducts
            .Where(product => !string.IsNullOrWhiteSpace(product.CategorySlug))
            .Select(product => product.CategorySlug!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(slug => slug, StringComparer.OrdinalIgnoreCase);

        foreach (var categorySlug in categorySlugs)
        {
            urlSet.Add(CreateUrlElement(ns, $"{baseUrl}/category/{categorySlug}"));
        }

        foreach (var product in activeProducts)
        {
            urlSet.Add(CreateUrlElement(ns, $"{baseUrl}/product/{product.Slug}"));
        }

        var blogResult = await contentClient.GetAllPublishedBlogSlugs(cancellationToken);
        if (blogResult.IsSuccess && blogResult.Value is not null)
        {
            foreach (var slug in blogResult.Value)
            {
                urlSet.Add(CreateUrlElement(ns, $"{baseUrl}/blog/{slug}"));
            }
        }

        var pagesResult = await contentClient.GetAllPublishedPageSlugs(cancellationToken);
        if (pagesResult.IsSuccess && pagesResult.Value is not null)
        {
            foreach (var slug in pagesResult.Value)
            {
                urlSet.Add(CreateUrlElement(ns, $"{baseUrl}/p/{slug}"));
            }
        }

        var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlSet);
        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement CreateUrlElement(XNamespace ns, string location)
    {
        return new XElement(
            ns + "url",
            new XElement(ns + "loc", location));
    }

    private static string NormalizeBaseUrl(string siteBaseUrl)
    {
        return string.IsNullOrWhiteSpace(siteBaseUrl)
            ? "http://localhost:5100"
            : siteBaseUrl.TrimEnd('/');
    }
}
