using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Storefront.Web.Services.Content;

public sealed class DirectusContentClient(
    HttpClient httpClient,
    IDistributedCache distributedCache,
    IOptions<CmsOptions> cmsOptions,
    ILogger<DirectusContentClient> logger)
    : IContentClient
{
    private const int MaxPageSize = 100;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly string apiKey = cmsOptions.Value.CmsApiKey.Trim();
    private readonly TimeSpan cacheDuration = TimeSpan.FromSeconds(Math.Clamp(cmsOptions.Value.CacheSeconds, 10, 3600));

    public Task<ContentFetchResult<IReadOnlyCollection<BlogPostContent>>> GetBlogPosts(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var cacheKey = $"cms:blog:index:{normalizedPage}:{normalizedPageSize}";

        return this.GetOrSetAsync(
            cacheKey,
            () => this.FetchBlogPostsAsync(normalizedPage, normalizedPageSize, cancellationToken),
            cancellationToken);
    }

    public Task<ContentFetchResult<BlogPostContent>> GetBlogPostBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var cacheKey = $"cms:blog:slug:{normalizedSlug}";

        return this.GetOrSetAsync(
            cacheKey,
            () => this.FetchBlogPostBySlugAsync(normalizedSlug, cancellationToken),
            cancellationToken);
    }

    public Task<ContentFetchResult<IReadOnlyCollection<LandingPageContent>>> GetPages(
        CancellationToken cancellationToken)
    {
        const string CacheKey = "cms:pages:all";
        return this.GetOrSetAsync(CacheKey, () => this.FetchPagesAsync(cancellationToken), cancellationToken);
    }

    public Task<ContentFetchResult<LandingPageContent>> GetPageBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var cacheKey = $"cms:page:slug:{normalizedSlug}";

        return this.GetOrSetAsync(
            cacheKey,
            () => this.FetchPageBySlugAsync(normalizedSlug, cancellationToken),
            cancellationToken);
    }

    private async Task<ContentFetchResult<IReadOnlyCollection<BlogPostContent>>> FetchBlogPostsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = QueryString.Create(
        [
            new KeyValuePair<string, string?>("fields", "title,slug,excerpt,content,coverImageUrl,authorName,publishedAt,updatedAt,tags,seoTitle,seoDescription,canonicalUrl,noIndex"),
            new KeyValuePair<string, string?>("sort", "-publishedAt"),
            new KeyValuePair<string, string?>("page", page.ToString(CultureInfo.InvariantCulture)),
            new KeyValuePair<string, string?>("limit", pageSize.ToString(CultureInfo.InvariantCulture)),
            new KeyValuePair<string, string?>("filter[publishedAt][_nnull]", "true"),
            new KeyValuePair<string, string?>("filter[publishedAt][_lte]", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
        ]);

        using var response = await this.SendAsync($"/items/blog_posts{query}", cancellationToken);
        if (response is null)
        {
            return ContentFetchResult<IReadOnlyCollection<BlogPostContent>>.Unavailable();
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("CMS blog index request failed with status code {StatusCode}", response.StatusCode);
            return ContentFetchResult<IReadOnlyCollection<BlogPostContent>>.Unavailable();
        }

        var payload = await response.Content.ReadFromJsonAsync<DirectusEnvelope<List<DirectusBlogPostDto>>>(
            SerializerOptions,
            cancellationToken);

        var posts = payload?.Data?
            .Select(MapBlogPost)
            .Where(item => item is not null)
            .Select(item => item!)
            .Where(IsPublished)
            .ToList()
            ?? [];

        return ContentFetchResult<IReadOnlyCollection<BlogPostContent>>.Success(posts);
    }

    private async Task<ContentFetchResult<BlogPostContent>> FetchBlogPostBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var query = QueryString.Create(
        [
            new KeyValuePair<string, string?>("fields", "title,slug,excerpt,content,coverImageUrl,authorName,publishedAt,updatedAt,tags,seoTitle,seoDescription,canonicalUrl,noIndex"),
            new KeyValuePair<string, string?>("filter[slug][_eq]", slug),
            new KeyValuePair<string, string?>("limit", "1"),
        ]);

        using var response = await this.SendAsync($"/items/blog_posts{query}", cancellationToken);
        if (response is null)
        {
            return ContentFetchResult<BlogPostContent>.Unavailable();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ContentFetchResult<BlogPostContent>.NotFound();
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("CMS blog post request failed with status code {StatusCode}", response.StatusCode);
            return ContentFetchResult<BlogPostContent>.Unavailable();
        }

        var payload = await response.Content.ReadFromJsonAsync<DirectusEnvelope<List<DirectusBlogPostDto>>>(
            SerializerOptions,
            cancellationToken);

        var item = payload?.Data?.Select(MapBlogPost).FirstOrDefault(post => post is not null);
        if (item is null || !IsPublished(item))
        {
            return ContentFetchResult<BlogPostContent>.NotFound();
        }

        return ContentFetchResult<BlogPostContent>.Success(item);
    }

    private async Task<ContentFetchResult<IReadOnlyCollection<LandingPageContent>>> FetchPagesAsync(
        CancellationToken cancellationToken)
    {
        var query = QueryString.Create(
        [
            new KeyValuePair<string, string?>("fields", "title,slug,content,seoTitle,seoDescription,canonicalUrl,noIndex"),
            new KeyValuePair<string, string?>("sort", "slug"),
            new KeyValuePair<string, string?>("limit", "500"),
        ]);

        using var response = await this.SendAsync($"/items/pages{query}", cancellationToken);
        if (response is null)
        {
            return ContentFetchResult<IReadOnlyCollection<LandingPageContent>>.Unavailable();
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("CMS pages request failed with status code {StatusCode}", response.StatusCode);
            return ContentFetchResult<IReadOnlyCollection<LandingPageContent>>.Unavailable();
        }

        var payload = await response.Content.ReadFromJsonAsync<DirectusEnvelope<List<DirectusPageDto>>>(
            SerializerOptions,
            cancellationToken);

        var pages = payload?.Data?
            .Select(MapLandingPage)
            .Where(page => page is not null)
            .Select(page => page!)
            .ToList()
            ?? [];

        return ContentFetchResult<IReadOnlyCollection<LandingPageContent>>.Success(pages);
    }

    private async Task<ContentFetchResult<LandingPageContent>> FetchPageBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var query = QueryString.Create(
        [
            new KeyValuePair<string, string?>("fields", "title,slug,content,seoTitle,seoDescription,canonicalUrl,noIndex"),
            new KeyValuePair<string, string?>("filter[slug][_eq]", slug),
            new KeyValuePair<string, string?>("limit", "1"),
        ]);

        using var response = await this.SendAsync($"/items/pages{query}", cancellationToken);
        if (response is null)
        {
            return ContentFetchResult<LandingPageContent>.Unavailable();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ContentFetchResult<LandingPageContent>.NotFound();
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("CMS page request failed with status code {StatusCode}", response.StatusCode);
            return ContentFetchResult<LandingPageContent>.Unavailable();
        }

        var payload = await response.Content.ReadFromJsonAsync<DirectusEnvelope<List<DirectusPageDto>>>(
            SerializerOptions,
            cancellationToken);

        var item = payload?.Data?.Select(MapLandingPage).FirstOrDefault(page => page is not null);
        return item is null
            ? ContentFetchResult<LandingPageContent>.NotFound()
            : ContentFetchResult<LandingPageContent>.Success(item);
    }

    private async Task<HttpResponseMessage?> SendAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        if (!string.IsNullOrWhiteSpace(this.apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.apiKey);
        }

        try
        {
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(exception, "CMS request failed for {RelativeUrl}", relativeUrl);
            return null;
        }
    }

    private async Task<ContentFetchResult<T>> GetOrSetAsync<T>(
        string cacheKey,
        Func<Task<ContentFetchResult<T>>> factory,
        CancellationToken cancellationToken)
    {
        var cached = await this.TryGetCacheAsync<ContentFetchResult<T>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var result = await factory();
        await this.TrySetCacheAsync(cacheKey, result, cancellationToken);
        return result;
    }

    private async Task<T?> TryGetCacheAsync<T>(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await distributedCache.GetStringAsync(cacheKey, cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read CMS cache key {CacheKey}", cacheKey);
            return default;
        }
    }

    private async Task TrySetCacheAsync<T>(string cacheKey, T value, CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Serialize(value, SerializerOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = this.cacheDuration,
            };

            await distributedCache.SetStringAsync(cacheKey, payload, options, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to write CMS cache key {CacheKey}", cacheKey);
        }
    }

    private bool IsPublished(BlogPostContent post)
    {
        return post.PublishedAt is not null && post.PublishedAt <= DateTimeOffset.UtcNow;
    }

    private BlogPostContent? MapBlogPost(DirectusBlogPostDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Slug))
        {
            return null;
        }

        var excerpt = string.IsNullOrWhiteSpace(dto.Excerpt)
            ? BuildFallbackExcerpt(dto.Content)
            : dto.Excerpt.Trim();

        return new BlogPostContent(
            dto.Title.Trim(),
            dto.Slug.Trim().ToLowerInvariant(),
            excerpt,
            dto.Content?.Trim() ?? string.Empty,
            dto.CoverImageUrl?.Trim(),
            string.IsNullOrWhiteSpace(dto.AuthorName) ? "Editorial Team" : dto.AuthorName.Trim(),
            dto.PublishedAt,
            dto.UpdatedAt,
            ParseTags(dto.Tags),
            dto.SeoTitle?.Trim(),
            dto.SeoDescription?.Trim(),
            dto.CanonicalUrl?.Trim(),
            dto.NoIndex ?? false);
    }

    private LandingPageContent? MapLandingPage(DirectusPageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Slug))
        {
            return null;
        }

        return new LandingPageContent(
            dto.Title.Trim(),
            dto.Slug.Trim().ToLowerInvariant(),
            dto.Content?.Trim() ?? string.Empty,
            dto.SeoTitle?.Trim(),
            dto.SeoDescription?.Trim(),
            dto.CanonicalUrl?.Trim(),
            dto.NoIndex ?? false);
    }

    private string BuildFallbackExcerpt(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Read the latest update from our blog.";
        }

        var trimmed = content.Trim();
        return trimmed.Length <= 220 ? trimmed : trimmed[..220];
    }

    private IReadOnlyCollection<string> ParseTags(JsonElement? tagsElement)
    {
        if (tagsElement is null || tagsElement.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (tagsElement.Value.ValueKind == JsonValueKind.String)
        {
            var raw = tagsElement.Value.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return [];
            }

            return raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (tagsElement.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var tags = new List<string>();
        foreach (var tagElement in tagsElement.Value.EnumerateArray())
        {
            if (tagElement.ValueKind == JsonValueKind.String)
            {
                var tag = tagElement.GetString();
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    tags.Add(tag.Trim());
                }

                continue;
            }

            if (tagElement.ValueKind != JsonValueKind.Object ||
                !tagElement.TryGetProperty("name", out var nameProperty))
            {
                continue;
            }

            var relationTag = nameProperty.GetString();
            if (!string.IsNullOrWhiteSpace(relationTag))
            {
                tags.Add(relationTag.Trim());
            }
        }

        return tags
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record DirectusEnvelope<T>(T Data);

    private sealed record DirectusBlogPostDto(
        string? Title,
        string? Slug,
        string? Excerpt,
        string? Content,
        string? CoverImageUrl,
        string? AuthorName,
        DateTimeOffset? PublishedAt,
        DateTimeOffset? UpdatedAt,
        JsonElement? Tags,
        string? SeoTitle,
        string? SeoDescription,
        string? CanonicalUrl,
        bool? NoIndex);

    private sealed record DirectusPageDto(
        string? Title,
        string? Slug,
        string? Content,
        string? SeoTitle,
        string? SeoDescription,
        string? CanonicalUrl,
        bool? NoIndex);
}
