namespace Storefront.Web.Services.Content;

public sealed record BlogPostContent(
    string Title,
    string Slug,
    string Excerpt,
    string Content,
    string? CoverImageUrl,
    string AuthorName,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyCollection<string> Tags,
    string? SeoTitle,
    string? SeoDescription,
    string? CanonicalUrl,
    bool NoIndex);
