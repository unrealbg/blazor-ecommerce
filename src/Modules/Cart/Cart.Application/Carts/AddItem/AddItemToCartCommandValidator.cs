using FluentValidation;

namespace Cart.Application.Carts.AddItem;

public sealed class AddItemToCartCommandValidator : AbstractValidator<AddItemToCartCommand>
{
    public AddItemToCartCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(command => command.ProductId)
            .NotEmpty();

        RuleFor(command => command.VariantId)
            .NotEmpty();

        RuleFor(command => command.Quantity)
            .GreaterThan(0);
    }
}
