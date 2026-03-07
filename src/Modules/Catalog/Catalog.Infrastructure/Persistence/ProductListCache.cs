using System.Text.Json;
using BuildingBlocks.Application.Contracts;
using Catalog.Application.Products;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.Infrastructure.Persistence;

internal sealed class ProductListCache(IDistributedCache distributedCache) : IProductListCache
{
    private const string CacheKey = "catalog:products:v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public async Task<IReadOnlyCollection<ProductSnapshot>?> GetAsync(CancellationToken cancellationToken)
    {
        var payload = await distributedCache.GetStringAsync(CacheKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<ProductSnapshot>>(payload);
    }

    public async Task SetAsync(IReadOnlyCollection<ProductSnapshot> products, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(products);
        await distributedCache.SetStringAsync(
            CacheKey,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration,
            },
            cancellationToken);
    }

    public Task InvalidateAsync(CancellationToken cancellationToken)
    {
        return distributedCache.RemoveAsync(CacheKey, cancellationToken);
    }
}
