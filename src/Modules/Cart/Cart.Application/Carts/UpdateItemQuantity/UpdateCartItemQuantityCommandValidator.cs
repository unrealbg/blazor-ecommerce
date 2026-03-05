using FluentValidation;

namespace Cart.Application.Carts.UpdateItemQuantity;

public sealed class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(command => command.ProductId)
            .NotEmpty();

        RuleFor(command => command.Quantity)
            .GreaterThan(0);
    }
}
