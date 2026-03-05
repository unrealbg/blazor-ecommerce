namespace Storefront.Web.Services.Seo;

public sealed record CanonicalUrls(
    string CanonicalUrl,
    string? PrevUrl,
    string? NextUrl,
    int CurrentPage);
