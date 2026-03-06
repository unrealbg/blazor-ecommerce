using FluentValidation;

namespace Shipping.Application.Shipping.QuoteShipping;

public sealed class QuoteShippingCommandValidator : AbstractValidator<QuoteShippingCommand>
{
    public QuoteShippingCommandValidator()
    {
        RuleFor(command => command.CountryCode)
            .NotEmpty()
            .Length(2);

        RuleFor(command => command.SubtotalAmount)
            .GreaterThanOrEqualTo(0m);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .Length(3);
    }
}
