namespace Storefront.Web.Services.Seo;

public interface IStructuredDataService
{
    string BuildProductJsonLd(ProductSeoModel model, string canonicalUrl);

    string BuildBreadcrumbJsonLd(IEnumerable<BreadcrumbItem> items);

    string BuildWebSiteSearchJsonLd(string siteBaseUrl);
}
