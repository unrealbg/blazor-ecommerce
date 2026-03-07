using Microsoft.Extensions.Options;
using Storefront.Web.Configuration;

namespace Storefront.Web.Services.Content;

public sealed class FeatureFlaggedContentClient(
    DirectusContentClient inner,
    IOptions<StorefrontFeatureFlagsOptions> featureFlags)
    : IContentClient
{
    private readonly StorefrontFeatureFlagsOptions featureFlags = featureFlags.Value;

    public Task<ContentFetchResult<IReadOnlyCollection<BlogPostContent>>> GetBlogPosts(int page, int pageSize, CancellationToken cancellationToken)
        => featureFlags.EnableCmsContent
            ? inner.GetBlogPosts(page, pageSize, cancellationToken)
            : Task.FromResult(ContentFetchResult<IReadOnlyCollection<BlogPostContent>>.Unavailable());

    public Task<ContentFetchResult<BlogPostContent>> GetBlogPostBySlug(string slug, CancellationToken cancellationToken)
        => featureFlags.EnableCmsContent
            ? inner.GetBlogPostBySlug(slug, cancellationToken)
            : Task.FromResult(ContentFetchResult<BlogPostContent>.Unavailable());

    public Task<ContentFetchResult<LandingPageContent>> GetPageBySlug(string slug, CancellationToken cancellationToken)
        => featureFlags.EnableCmsContent
            ? inner.GetPageBySlug(slug, cancellationToken)
            : Task.FromResult(ContentFetchResult<LandingPageContent>.Unavailable());

    public Task<ContentFetchResult<IReadOnlyCollection<string>>> GetAllPublishedBlogSlugs(CancellationToken cancellationToken)
        => featureFlags.EnableCmsContent
            ? inner.GetAllPublishedBlogSlugs(cancellationToken)
            : Task.FromResult(ContentFetchResult<IReadOnlyCollection<string>>.Unavailable());

    public Task<ContentFetchResult<IReadOnlyCollection<string>>> GetAllPublishedPageSlugs(CancellationToken cancellationToken)
        => featureFlags.EnableCmsContent
            ? inner.GetAllPublishedPageSlugs(cancellationToken)
            : Task.FromResult(ContentFetchResult<IReadOnlyCollection<string>>.Unavailable());
}