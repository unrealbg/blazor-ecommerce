using BuildingBlocks.Application.Abstractions;
using Redirects.Domain.RedirectRules;

namespace Redirects.Application.RedirectRules.CreateRedirectRule;

public sealed record CreateRedirectRuleCommand(
    string FromPath,
    string ToPath,
    int StatusCode = RedirectStatusCodes.PermanentRedirect) : ICommand<Guid>;
