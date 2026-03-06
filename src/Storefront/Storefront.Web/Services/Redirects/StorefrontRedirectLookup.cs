using Microsoft.Extensions.Caching.Memory;
using Storefront.Web.Services.Api;

namespace Storefront.Web.Services.Redirects;

public sealed class StorefrontRedirectLookup(
    IStoreApiClient storeApiClient,
    IMemoryCache memoryCache,
    ILogger<StorefrontRedirectLookup> logger)
    : IStorefrontRedirectLookup
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public async Task<StorefrontRedirectMatch?> ResolveAsync(string requestPath, CancellationToken cancellationToken)
    {
        var normalizedPath = NormalizePath(requestPath);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        var cacheKey = $"storefront:redirect:{normalizedPath}";
        if (memoryCache.TryGetValue(cacheKey, out CacheItem? cacheItem) && cacheItem is not null)
        {
            return cacheItem.Match;
        }

        try
        {
            var response = await storeApiClient.ResolveRedirectAsync(normalizedPath, cancellationToken);
            var match = response is null
                ? null
                : new StorefrontRedirectMatch(response.FromPath, response.ToPath, response.StatusCode);

            memoryCache.Set(cacheKey, new CacheItem(match), CacheDuration);
            return match;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redirect resolve API request failed for path {Path}", normalizedPath);
            return null;
        }
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Trim().ToLowerInvariant();

        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = $"/{normalized}";
        }

        if (normalized.Length > 1)
        {
            normalized = normalized.TrimEnd('/');
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = $"/{normalized}";
            }
        }

        return normalized;
    }

    private sealed record CacheItem(StorefrontRedirectMatch? Match);
}
