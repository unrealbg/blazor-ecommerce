using FluentValidation;

namespace Search.Application.Search;

public sealed class SuggestProductsQueryValidator : AbstractValidator<SuggestProductsQuery>
{
    public SuggestProductsQueryValidator()
    {
        RuleFor(query => query.Query)
            .MaximumLength(200);
    }
}
