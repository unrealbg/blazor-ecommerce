using BuildingBlocks.Application.Abstractions;

namespace Redirects.Application.RedirectRules.DeactivateRedirectRule;

public sealed record DeactivateRedirectRuleCommand(Guid RedirectRuleId) : ICommand;
