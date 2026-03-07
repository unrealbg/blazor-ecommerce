using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Storefront.Web.Services.Caching;

internal static class DistributedCacheJsonExtensions
{
    public static async Task<T?> GetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken)
    {
        var payload = await cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(payload, serializerOptions);
    }

    public static Task SetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        TimeSpan ttl,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(value, serializerOptions);
        return cache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
            },
            cancellationToken);
    }
}