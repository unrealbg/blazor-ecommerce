using FluentValidation;

namespace Redirects.Application.RedirectRules.CreateRedirectRule;

public sealed class CreateRedirectRuleCommandValidator : AbstractValidator<CreateRedirectRuleCommand>
{
    public CreateRedirectRuleCommandValidator()
    {
        RuleFor(command => command.FromPath)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(command => command.ToPath)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(command => command.StatusCode)
            .Must(statusCode => statusCode is 301 or 302 or 307 or 308)
            .WithMessage("Supported redirect status codes are 301, 302, 307 and 308.");
    }
}
