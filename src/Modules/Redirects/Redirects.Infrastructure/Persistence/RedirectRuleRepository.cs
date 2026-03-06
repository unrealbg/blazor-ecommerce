using Microsoft.EntityFrameworkCore;
using Redirects.Application.RedirectRules;
using Redirects.Domain.RedirectRules;

namespace Redirects.Infrastructure.Persistence;

internal sealed class RedirectRuleRepository(RedirectsDbContext dbContext) : IRedirectRuleRepository
{
    public Task AddAsync(RedirectRule redirectRule, CancellationToken cancellationToken)
    {
        return dbContext.RedirectRules.AddAsync(redirectRule, cancellationToken).AsTask();
    }

    public Task<RedirectRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.RedirectRules
            .SingleOrDefaultAsync(redirectRule => redirectRule.Id == id, cancellationToken);
    }

    public Task<RedirectRule?> GetActiveByFromPathAsync(string normalizedFromPath, CancellationToken cancellationToken)
    {
        return dbContext.RedirectRules
            .SingleOrDefaultAsync(
                redirectRule => redirectRule.FromPath == normalizedFromPath && redirectRule.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<RedirectMatch>> ListActiveMatchesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.RedirectRules
            .AsNoTracking()
            .Where(redirectRule => redirectRule.IsActive)
            .Select(redirectRule => new RedirectMatch(
                redirectRule.FromPath,
                redirectRule.ToPath,
                redirectRule.StatusCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RedirectRule>> ListPageAsync(int skip, int take, CancellationToken cancellationToken)
    {
        return await dbContext.RedirectRules
            .AsNoTracking()
            .OrderByDescending(redirectRule => redirectRule.UpdatedAtUtc)
            .ThenBy(redirectRule => redirectRule.FromPath)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<long> CountAsync(CancellationToken cancellationToken)
    {
        return dbContext.RedirectRules.LongCountAsync(cancellationToken);
    }

    public async Task IncrementHitCountAsync(
        string normalizedFromPath,
        long increment,
        DateTime lastHitAtUtc,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE redirects.redirect_rules
             SET hit_count = hit_count + {increment},
                 last_hit_at = {lastHitAtUtc},
                 updated_at = {lastHitAtUtc}
             WHERE from_path = {normalizedFromPath} AND is_active = TRUE
             """,
            cancellationToken);
    }
}
