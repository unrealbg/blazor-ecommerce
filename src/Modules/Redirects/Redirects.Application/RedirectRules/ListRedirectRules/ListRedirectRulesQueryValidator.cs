using FluentValidation;

namespace Redirects.Application.RedirectRules.ListRedirectRules;

public sealed class ListRedirectRulesQueryValidator : AbstractValidator<ListRedirectRulesQuery>
{
    public ListRedirectRulesQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200);
    }
}
