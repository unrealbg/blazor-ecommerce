using FluentValidation;

namespace Cart.Application.Carts.CreateCart;

public sealed class CreateCartCommandValidator : AbstractValidator<CreateCartCommand>
{
    public CreateCartCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .NotEmpty();
    }
}
