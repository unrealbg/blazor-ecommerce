using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Redirects.Application.RedirectRules;
using StackExchange.Redis;

namespace Redirects.Infrastructure.Persistence;

internal sealed class RedirectRuleCache(
    IMemoryCache memoryCache,
    IRedirectRuleRepository redirectRuleRepository,
    ILogger<RedirectRuleCache> logger,
    IConnectionMultiplexer? redisConnection = null)
    : IRedirectRuleCache
{
    private const string MemoryCacheKey = "redirects:rules:memory";
    private const string RedisHashKey = "redirects:rules";
    private static readonly TimeSpan MemoryCacheDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RedisCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim CacheRefreshSemaphore = new(1, 1);

    public async Task<RedirectMatch?> GetAsync(string normalizedFromPath, CancellationToken cancellationToken)
    {
        var rules = await GetRulesAsync(cancellationToken);
        return rules.TryGetValue(normalizedFromPath, out var redirectMatch)
            ? redirectMatch
            : null;
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken)
    {
        memoryCache.Remove(MemoryCacheKey);

        if (redisConnection is null || !redisConnection.IsConnected)
        {
            return;
        }

        try
        {
            _ = cancellationToken;
            await redisConnection.GetDatabase().KeyDeleteAsync(RedisHashKey);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Could not invalidate redirects redis hash cache.");
        }
    }

    private async Task<IReadOnlyDictionary<string, RedirectMatch>> GetRulesAsync(CancellationToken cancellationToken)
    {
        if (memoryCache.TryGetValue(MemoryCacheKey, out IReadOnlyDictionary<string, RedirectMatch>? cachedRules) &&
            cachedRules is not null)
        {
            return cachedRules;
        }

        await CacheRefreshSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (memoryCache.TryGetValue(MemoryCacheKey, out cachedRules) && cachedRules is not null)
            {
                return cachedRules;
            }

            var rules = await LoadFromRedisAsync(cancellationToken) ?? await LoadFromDatabaseAsync(cancellationToken);
            memoryCache.Set(MemoryCacheKey, rules, MemoryCacheDuration);
            return rules;
        }
        finally
        {
            CacheRefreshSemaphore.Release();
        }
    }

    private async Task<IReadOnlyDictionary<string, RedirectMatch>?> LoadFromRedisAsync(CancellationToken cancellationToken)
    {
        if (redisConnection is null || !redisConnection.IsConnected)
        {
            return null;
        }

        try
        {
            _ = cancellationToken;
            var entries = await redisConnection.GetDatabase().HashGetAllAsync(RedisHashKey);
            if (entries.Length == 0)
            {
                return null;
            }

            var dictionary = new Dictionary<string, RedirectMatch>(entries.Length, StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (!TryParseRedisEntry(entry.Name, entry.Value, out var redirectMatch))
                {
                    continue;
                }

                dictionary[entry.Name!] = redirectMatch;
            }

            return dictionary;
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Could not load redirects from redis hash cache.");
            return null;
        }
    }

    private async Task<IReadOnlyDictionary<string, RedirectMatch>> LoadFromDatabaseAsync(CancellationToken cancellationToken)
    {
        var activeRules = await redirectRuleRepository.ListActiveMatchesAsync(cancellationToken);

        var dictionary = activeRules
            .ToDictionary(match => match.FromPath, match => match, StringComparer.Ordinal);

        if (redisConnection is not null && redisConnection.IsConnected)
        {
            try
            {
                var redisDatabase = redisConnection.GetDatabase();

                if (dictionary.Count == 0)
                {
                    await redisDatabase.KeyDeleteAsync(RedisHashKey);
                }
                else
                {
                    var entries = dictionary
                        .Select(item => new HashEntry(item.Key, BuildRedisValue(item.Value)))
                        .ToArray();

                    await redisDatabase.HashSetAsync(RedisHashKey, entries);
                    await redisDatabase.KeyExpireAsync(RedisHashKey, RedisCacheDuration);
                }
            }
            catch (RedisException exception)
            {
                logger.LogWarning(exception, "Could not refresh redirects redis hash cache from database.");
            }
        }

        return dictionary;
    }

    private string BuildRedisValue(RedirectMatch redirectMatch)
    {
        return $"{redirectMatch.ToPath}|{redirectMatch.StatusCode}";
    }

    private bool TryParseRedisEntry(RedisValue field, RedisValue value, out RedirectMatch redirectMatch)
    {
        var serialized = value.ToString();
        var separatorIndex = serialized.LastIndexOf('|');
        if (separatorIndex <= 0 || separatorIndex == serialized.Length - 1)
        {
            redirectMatch = null!;
            return false;
        }

        var toPath = serialized[..separatorIndex];
        var statusCodeText = serialized[(separatorIndex + 1)..];
        if (!int.TryParse(statusCodeText, out var statusCode))
        {
            redirectMatch = null!;
            return false;
        }

        redirectMatch = new RedirectMatch(field!, toPath, statusCode);
        return true;
    }
}
