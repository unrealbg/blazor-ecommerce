using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using Redirects.Application.RedirectRules;
using Redirects.Domain.RedirectRules;

namespace Redirects.Infrastructure.Persistence;

internal sealed class RedirectRuleWriter(
    IRedirectRuleRepository redirectRuleRepository,
    IRedirectsUnitOfWork unitOfWork,
    IRedirectRuleCache redirectRuleCache,
    IClock clock)
    : IRedirectRuleWriter
{
    public async Task UpsertAsync(string fromPath, string toPath, int statusCode, CancellationToken cancellationToken)
    {
        var normalizedFromResult = RedirectPathNormalizer.NormalizeFromPath(fromPath);
        if (normalizedFromResult.IsFailure)
        {
            return;
        }

        var normalizedToResult = RedirectPathNormalizer.NormalizeToPath(toPath);
        if (normalizedToResult.IsFailure)
        {
            return;
        }

        if (string.Equals(
                normalizedFromResult.Value,
                RedirectPathNormalizer.NormalizeForComparison(normalizedToResult.Value),
                StringComparison.Ordinal))
        {
            return;
        }

        var existingRule = await redirectRuleRepository.GetActiveByFromPathAsync(
            normalizedFromResult.Value,
            cancellationToken);

        if (existingRule is null)
        {
            var createResult = RedirectRule.Create(
                normalizedFromResult.Value,
                normalizedToResult.Value,
                statusCode,
                clock.UtcNow);

            if (createResult.IsFailure)
            {
                return;
            }

            await redirectRuleRepository.AddAsync(createResult.Value, cancellationToken);
        }
        else
        {
            var updateResult = existingRule.UpdateTarget(normalizedToResult.Value, statusCode, clock.UtcNow);
            if (updateResult.IsFailure)
            {
                return;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await redirectRuleCache.InvalidateAsync(cancellationToken);
    }
}
