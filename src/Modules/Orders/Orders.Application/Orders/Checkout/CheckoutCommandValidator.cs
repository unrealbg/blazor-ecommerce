using FluentValidation;

namespace Orders.Application.Orders.Checkout;

public sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128);
    }
}
