using FluentValidation;

namespace Payments.Application.Payments.RefundPaymentIntent;

public sealed class RefundPaymentIntentCommandValidator : AbstractValidator<RefundPaymentIntentCommand>
{
    public RefundPaymentIntentCommandValidator()
    {
        RuleFor(command => command.PaymentIntentId).NotEmpty();
        RuleFor(command => command.Amount)
            .GreaterThan(0m)
            .When(command => command.Amount.HasValue);
        RuleFor(command => command.Reason).MaximumLength(512);
    }
}
