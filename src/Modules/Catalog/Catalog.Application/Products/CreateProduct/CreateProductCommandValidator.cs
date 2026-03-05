using FluentValidation;

namespace Catalog.Application.Products.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private const int NameMaxLength = 200;
    private const int DescriptionMaxLength = 2000;
    private const int BrandMaxLength = 120;
    private const int SkuMaxLength = 64;
    private const int ImageUrlMaxLength = 2000;
    private const int CategorySlugMaxLength = 120;
    private const int CategoryNameMaxLength = 120;

    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(NameMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(DescriptionMaxLength);

        RuleFor(command => command.Brand)
            .MaximumLength(BrandMaxLength);

        RuleFor(command => command.Sku)
            .MaximumLength(SkuMaxLength);

        RuleFor(command => command.ImageUrl)
            .MaximumLength(ImageUrlMaxLength);

        RuleFor(command => command.CategorySlug)
            .MaximumLength(CategorySlugMaxLength);

        RuleFor(command => command.CategoryName)
            .MaximumLength(CategoryNameMaxLength);

        RuleFor(command => command)
            .Must(command => (string.IsNullOrWhiteSpace(command.CategorySlug) &&
                              string.IsNullOrWhiteSpace(command.CategoryName)) ||
                             (!string.IsNullOrWhiteSpace(command.CategorySlug) &&
                              !string.IsNullOrWhiteSpace(command.CategoryName)))
            .WithMessage("Category slug and category name must both be provided.");

        RuleFor(command => command.Currency)
            .NotEmpty()
            .Length(3)
            .Must(currency => string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Currency must be EUR.");

        RuleFor(command => command.Amount)
            .GreaterThanOrEqualTo(0m);
    }
}
