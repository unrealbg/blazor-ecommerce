using FluentValidation;

namespace Payments.Application.Payments.CreatePaymentIntent;

public sealed class CreatePaymentIntentCommandValidator : AbstractValidator<CreatePaymentIntentCommand>
{
    public CreatePaymentIntentCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Provider).MaximumLength(64);
        RuleFor(command => command.CustomerEmail).MaximumLength(256);
    }
}
