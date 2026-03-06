using FluentValidation;

namespace Shipping.Application.Shipping.CreateShippingRateRule;

public sealed class CreateShippingRateRuleCommandValidator : AbstractValidator<CreateShippingRateRuleCommand>
{
    public CreateShippingRateRuleCommandValidator()
    {
        RuleFor(command => command.ShippingMethodId).NotEmpty();
        RuleFor(command => command.ShippingZoneId).NotEmpty();
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.PriceAmount).GreaterThanOrEqualTo(0m);
    }
}
