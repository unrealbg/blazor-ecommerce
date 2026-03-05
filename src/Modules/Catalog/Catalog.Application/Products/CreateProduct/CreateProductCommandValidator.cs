using FluentValidation;

namespace Catalog.Application.Products.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private const int NameMaxLength = 200;
    private const int DescriptionMaxLength = 2000;

    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(NameMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(DescriptionMaxLength);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(command => command.Amount)
            .GreaterThanOrEqualTo(0m);
    }
}
