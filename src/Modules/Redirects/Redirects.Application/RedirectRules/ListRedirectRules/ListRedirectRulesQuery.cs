using BuildingBlocks.Application.Abstractions;

namespace Redirects.Application.RedirectRules.ListRedirectRules;

public sealed record ListRedirectRulesQuery(int Page = 1, int PageSize = 20) : IQuery<RedirectRulePage>;
