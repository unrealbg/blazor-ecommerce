using BuildingBlocks.Application.Abstractions;

namespace Redirects.Application.RedirectRules.ListRedirectRules;

public sealed class ListRedirectRulesQueryHandler(IRedirectRuleRepository redirectRuleRepository)
    : IQueryHandler<ListRedirectRulesQuery, RedirectRulePage>
{
    public async Task<RedirectRulePage> Handle(ListRedirectRulesQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var redirectRules = await redirectRuleRepository.ListPageAsync(skip, request.PageSize, cancellationToken);
        var totalCount = await redirectRuleRepository.CountAsync(cancellationToken);

        var items = redirectRules
            .Select(redirectRule => new RedirectRuleListItem(
                redirectRule.Id,
                redirectRule.FromPath,
                redirectRule.ToPath,
                redirectRule.StatusCode,
                redirectRule.IsActive,
                redirectRule.HitCount,
                redirectRule.CreatedAtUtc,
                redirectRule.UpdatedAtUtc,
                redirectRule.LastHitAtUtc))
            .ToList();

        return new RedirectRulePage(request.Page, request.PageSize, totalCount, items);
    }
}
