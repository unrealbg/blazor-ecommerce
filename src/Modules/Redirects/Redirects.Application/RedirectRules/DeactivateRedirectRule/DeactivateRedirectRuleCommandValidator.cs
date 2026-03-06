using FluentValidation;

namespace Redirects.Application.RedirectRules.DeactivateRedirectRule;

public sealed class DeactivateRedirectRuleCommandValidator : AbstractValidator<DeactivateRedirectRuleCommand>
{
    public DeactivateRedirectRuleCommandValidator()
    {
        RuleFor(command => command.RedirectRuleId)
            .NotEmpty();
    }
}
