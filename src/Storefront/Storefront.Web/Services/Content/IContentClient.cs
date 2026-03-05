namespace Storefront.Web.Services.Content;

public interface IContentClient
{
    Task<ContentFetchResult<IReadOnlyCollection<BlogPostContent>>> GetBlogPosts(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ContentFetchResult<BlogPostContent>> GetBlogPostBySlug(
        string slug,
        CancellationToken cancellationToken);

    Task<ContentFetchResult<IReadOnlyCollection<LandingPageContent>>> GetPages(
        CancellationToken cancellationToken);

    Task<ContentFetchResult<LandingPageContent>> GetPageBySlug(
        string slug,
        CancellationToken cancellationToken);
}
