using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Redirects.Domain.RedirectRules;

namespace Redirects.Application.RedirectRules.CreateRedirectRule;

public sealed class CreateRedirectRuleCommandHandler(
    IRedirectRuleRepository redirectRuleRepository,
    IRedirectsUnitOfWork unitOfWork,
    IRedirectRuleCache redirectRuleCache,
    IClock clock)
    : ICommandHandler<CreateRedirectRuleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateRedirectRuleCommand request, CancellationToken cancellationToken)
    {
        var normalizedFromResult = RedirectPathNormalizer.NormalizeFromPath(request.FromPath);
        if (normalizedFromResult.IsFailure)
        {
            return Result<Guid>.Failure(normalizedFromResult.Error);
        }

        var normalizedToResult = RedirectPathNormalizer.NormalizeToPath(request.ToPath);
        if (normalizedToResult.IsFailure)
        {
            return Result<Guid>.Failure(normalizedToResult.Error);
        }

        if (string.Equals(
                normalizedFromResult.Value,
                RedirectPathNormalizer.NormalizeForComparison(normalizedToResult.Value),
                StringComparison.Ordinal))
        {
            return Result<Guid>.Failure(new Error(
                "redirects.rule.loop",
                "Redirect source and destination must not point to the same normalized path."));
        }

        var existingRule = await redirectRuleRepository.GetActiveByFromPathAsync(
            normalizedFromResult.Value,
            cancellationToken);

        if (existingRule is null)
        {
            var createResult = RedirectRule.Create(
                normalizedFromResult.Value,
                normalizedToResult.Value,
                request.StatusCode,
                clock.UtcNow);

            if (createResult.IsFailure)
            {
                return Result<Guid>.Failure(createResult.Error);
            }

            await redirectRuleRepository.AddAsync(createResult.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await redirectRuleCache.InvalidateAsync(cancellationToken);

            return Result<Guid>.Success(createResult.Value.Id);
        }

        var updateResult = existingRule.UpdateTarget(normalizedToResult.Value, request.StatusCode, clock.UtcNow);
        if (updateResult.IsFailure)
        {
            return Result<Guid>.Failure(updateResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await redirectRuleCache.InvalidateAsync(cancellationToken);

        return Result<Guid>.Success(existingRule.Id);
    }
}
