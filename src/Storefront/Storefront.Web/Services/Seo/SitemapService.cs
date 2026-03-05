using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Storefront.Web.Services.Api;
using Storefront.Web.Services.Content;

namespace Storefront.Web.Services.Seo;

public sealed class SitemapService(
    IStoreApiClient storeApiClient,
    IContentClient contentClient,
    IOptions<SeoOptions> seoOptions)
    : ISitemapService
{
    private readonly string baseUrl = NormalizeBaseUrl(seoOptions.Value.SiteBaseUrl);

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

        var blogResult = await contentClient.GetBlogPosts(1, 500, cancellationToken);
        if (blogResult.IsSuccess && blogResult.Value is not null)
        {
            foreach (var post in blogResult.Value.Where(post => post.PublishedAt is not null))
            {
                urlSet.Add(CreateUrlElement(ns, $"{baseUrl}/blog/{post.Slug}"));
            }
        }

        var pagesResult = await contentClient.GetPages(cancellationToken);
        if (pagesResult.IsSuccess && pagesResult.Value is not null)
        {
            foreach (var page in pagesResult.Value.Where(page => !page.NoIndex))
            {
                urlSet.Add(CreateUrlElement(ns, $"{baseUrl}/p/{page.Slug}"));
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
