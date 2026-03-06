using System.Text.Json;
using BuildingBlocks.Application.Contracts;
using Customers.Domain.Customers;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Customers.Infrastructure.Sessions;

internal sealed class CustomerSessionCache(
    IDistributedCache distributedCache,
    CustomersDbContext dbContext)
    : ICustomerSessionCache
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(24);

    public Task TouchCartSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var key = $"sessions:cart:{sessionId.Trim()}";
        return TryWriteCacheAsync(key, cancellationToken);
    }

    public async Task TouchCustomerSessionAsync(
        Guid customerId,
        string sessionId,
        CancellationToken cancellationToken)
    {
        await TryWriteCacheAsync($"sessions:cart:{sessionId.Trim()}", cancellationToken);
        await TryWriteCacheAsync($"sessions:customer:{customerId:N}:{sessionId.Trim()}", cancellationToken);

        var normalizedSessionId = sessionId.Trim();
        var existingSession = await dbContext.CustomerSessions
            .FirstOrDefaultAsync(session => session.SessionId == normalizedSessionId, cancellationToken);

        if (existingSession is null)
        {
            dbContext.CustomerSessions.Add(CustomerSession.Create(normalizedSessionId, customerId));
        }
        else
        {
            existingSession.Touch(customerId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task WriteCacheAsync(string key, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new SessionPayload(DateTime.UtcNow));

        await distributedCache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = SessionTtl,
            },
            cancellationToken);
    }

    private async Task TryWriteCacheAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await WriteCacheAsync(key, cancellationToken);
        }
        catch
        {
            // Session cache is best-effort. Redis outages must not block checkout/profile flows.
        }
    }

    private sealed record SessionPayload(DateTime TouchedAtUtc);
}
