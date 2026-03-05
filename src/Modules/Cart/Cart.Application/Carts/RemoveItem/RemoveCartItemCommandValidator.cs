using FluentValidation;

namespace Cart.Application.Carts.RemoveItem;

public sealed class RemoveCartItemCommandValidator : AbstractValidator<RemoveCartItemCommand>
{
    public RemoveCartItemCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(command => command.ProductId)
            .NotEmpty();
    }
}
