namespace Storefront.Web.Services.Seo;

public sealed record BlogPostingSeoModel(
    string Headline,
    string Description,
    string? ImageUrl,
    DateTimeOffset DatePublished,
    DateTimeOffset DateModified,
    string AuthorName);
