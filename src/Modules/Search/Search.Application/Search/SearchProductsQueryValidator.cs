using FluentValidation;

namespace Search.Application.Search;

public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsQueryValidator()
    {
        RuleFor(query => query.Query)
            .MaximumLength(200)
            .When(query => !string.IsNullOrWhiteSpace(query.Query));

        RuleFor(query => query.Sort)
            .Must(BeSupportedSort)
            .When(query => !string.IsNullOrWhiteSpace(query.Sort))
            .WithMessage("Unsupported sort option.");

        RuleFor(query => query.Brands)
            .Must(brands => brands is null || brands.All(brand => brand is null || brand.Length <= 120))
            .WithMessage("Brand filter value is too long.");
    }

    private static bool BeSupportedSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return true;
        }

        return SearchSortOptions.SupportedValues.Contains(sort.Trim().ToLowerInvariant(), StringComparer.Ordinal);
    }
}
