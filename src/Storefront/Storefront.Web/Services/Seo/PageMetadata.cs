namespace Storefront.Web.Services.Seo;

public sealed record PageMetadata(
    string Title,
    string Description,
    string CanonicalUrl,
    bool NoIndex = false,
    string OpenGraphType = "website",
    string? OpenGraphImageUrl = null);
