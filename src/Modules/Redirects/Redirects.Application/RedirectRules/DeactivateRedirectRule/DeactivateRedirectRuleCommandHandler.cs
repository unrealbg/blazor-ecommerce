using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Redirects.Application.RedirectRules.DeactivateRedirectRule;

public sealed class DeactivateRedirectRuleCommandHandler(
    IRedirectRuleRepository redirectRuleRepository,
    IRedirectsUnitOfWork unitOfWork,
    IRedirectRuleCache redirectRuleCache,
    IClock clock)
    : ICommandHandler<DeactivateRedirectRuleCommand>
{
    public async Task<Result> Handle(DeactivateRedirectRuleCommand request, CancellationToken cancellationToken)
    {
        var redirectRule = await redirectRuleRepository.GetByIdAsync(request.RedirectRuleId, cancellationToken);
        if (redirectRule is null)
        {
            return Result.Failure(new Error("redirects.rule.not_found", "Redirect rule was not found."));
        }

        if (!redirectRule.IsActive)
        {
            return Result.Success();
        }

        redirectRule.Deactivate(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await redirectRuleCache.InvalidateAsync(cancellationToken);

        return Result.Success();
    }
}
