using Redirects.Domain.RedirectRules;

namespace Redirects.Application.RedirectRules;

public interface IRedirectRuleRepository
{
    Task AddAsync(RedirectRule redirectRule, CancellationToken cancellationToken);

    Task<RedirectRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<RedirectRule?> GetActiveByFromPathAsync(string normalizedFromPath, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RedirectMatch>> ListActiveMatchesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RedirectRule>> ListPageAsync(int skip, int take, CancellationToken cancellationToken);

    Task<long> CountAsync(CancellationToken cancellationToken);

    Task IncrementHitCountAsync(
        string normalizedFromPath,
        long increment,
        DateTime lastHitAtUtc,
        CancellationToken cancellationToken);
}
