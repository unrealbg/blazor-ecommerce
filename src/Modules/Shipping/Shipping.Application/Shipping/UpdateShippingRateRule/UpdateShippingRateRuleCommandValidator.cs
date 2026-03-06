using FluentValidation;

namespace Shipping.Application.Shipping.UpdateShippingRateRule;

public sealed class UpdateShippingRateRuleCommandValidator : AbstractValidator<UpdateShippingRateRuleCommand>
{
    public UpdateShippingRateRuleCommandValidator()
    {
        RuleFor(command => command.ShippingRateRuleId).NotEmpty();
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.PriceAmount).GreaterThanOrEqualTo(0m);
    }
}
