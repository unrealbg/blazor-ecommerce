using FluentValidation;

namespace Redirects.Application.RedirectRules.ResolveRedirect;

public sealed class ResolveRedirectQueryValidator : AbstractValidator<ResolveRedirectQuery>
{
    public ResolveRedirectQueryValidator()
    {
        RuleFor(query => query.Path)
            .NotEmpty()
            .MaximumLength(500);
    }
}
