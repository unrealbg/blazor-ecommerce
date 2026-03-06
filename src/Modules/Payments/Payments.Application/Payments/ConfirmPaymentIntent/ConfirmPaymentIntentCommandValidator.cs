using FluentValidation;

namespace Payments.Application.Payments.ConfirmPaymentIntent;

public sealed class ConfirmPaymentIntentCommandValidator : AbstractValidator<ConfirmPaymentIntentCommand>
{
    public ConfirmPaymentIntentCommandValidator()
    {
        RuleFor(command => command.PaymentIntentId).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(200);
    }
}
