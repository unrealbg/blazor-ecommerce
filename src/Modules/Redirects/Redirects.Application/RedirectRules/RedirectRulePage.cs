namespace Redirects.Application.RedirectRules;

public sealed record RedirectRulePage(
    int Page,
    int PageSize,
    long TotalCount,
    IReadOnlyCollection<RedirectRuleListItem> Items);
