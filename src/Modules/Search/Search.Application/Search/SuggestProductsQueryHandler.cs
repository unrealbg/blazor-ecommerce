using System.Text.Json;
using BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Search.Application.Search;

public sealed class SuggestProductsQueryHandler(
    ISearchProvider searchProvider,
    IDistributedCache distributedCache)
    : IQueryHandler<SuggestProductsQuery, SearchSuggestionsResponse>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<SearchSuggestionsResponse> Handle(SuggestProductsQuery request, CancellationToken cancellationToken)
    {
        var normalizedQuery = request.Query.Trim();
        if (normalizedQuery.Length < 2)
        {
            return new SearchSuggestionsResponse(normalizedQuery, []);
        }

        var limit = Math.Clamp(request.Limit, 1, 10);
        var cacheKey = $"search:suggest:{normalizedQuery.ToLowerInvariant()}:{limit}";

        var cached = await TryGetFromCacheAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return new SearchSuggestionsResponse(normalizedQuery, cached);
        }

        var suggestions = await searchProvider.SuggestAsync(normalizedQuery, limit, cancellationToken);
        await TrySetCacheAsync(cacheKey, suggestions, cancellationToken);

        return new SearchSuggestionsResponse(normalizedQuery, suggestions);
    }

    private async Task<IReadOnlyCollection<SearchSuggestionItem>?> TryGetFromCacheAsync(
        string cacheKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = await distributedCache.GetStringAsync(cacheKey, cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            return JsonSerializer.Deserialize<IReadOnlyCollection<SearchSuggestionItem>>(payload, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    private async Task TrySetCacheAsync(
        string cacheKey,
        IReadOnlyCollection<SearchSuggestionItem> items,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Serialize(items, SerializerOptions);
            await distributedCache.SetStringAsync(
                cacheKey,
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20),
                },
                cancellationToken);
        }
        catch
        {
            // Best effort cache write.
        }
    }
}
