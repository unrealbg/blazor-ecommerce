using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Storefront.Web.Services.Api;

namespace Storefront.Web.Services.Seo;

public sealed class SitemapService(
    IStoreApiClient storeApiClient,
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

        foreach (var product in products
                     .Where(product => product.IsActive)
                     .Where(product => string.Equals(product.Currency, "EUR", StringComparison.Ordinal)))
        {
            urlSet.Add(CreateUrlElement(ns, $"{baseUrl}/product/{product.Slug}"));
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
