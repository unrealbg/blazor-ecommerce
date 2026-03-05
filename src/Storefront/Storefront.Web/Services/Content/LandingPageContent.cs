namespace Storefront.Web.Services.Content;

public sealed record LandingPageContent(
    string Title,
    string Slug,
    string Content,
    string? SeoTitle,
    string? SeoDescription,
    string? CanonicalUrl,
    bool NoIndex);
