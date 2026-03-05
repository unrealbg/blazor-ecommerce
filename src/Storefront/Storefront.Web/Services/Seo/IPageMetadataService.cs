namespace Storefront.Web.Services.Seo;

public interface IPageMetadataService
{
    PageMetadata ForHome();

    PageMetadata ForCategory(string categoryName, string categorySlug);

    PageMetadata ForProduct(string productName, string? description, string productSlug);

    PageMetadata ForSearch(string query);

    string ResolveCanonicalUrl(string? preferredCanonicalUrl, string fallbackRelativePath);

    string? ResolveAbsoluteOptionalUrl(string? value);

    string BuildAbsoluteUrl(string relativePathAndQuery);
}
