using FluentValidation;

namespace Cart.Application.Carts.CheckoutCart;

public sealed class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(command => command.CartId)
            .NotEmpty();

        RuleFor(command => command.Currency)
            .NotEmpty()
            .Length(3);
    }
}
