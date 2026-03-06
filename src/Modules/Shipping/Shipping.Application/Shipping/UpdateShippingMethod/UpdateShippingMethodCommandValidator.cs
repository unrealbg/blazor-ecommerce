using FluentValidation;

namespace Shipping.Application.Shipping.UpdateShippingMethod;

public sealed class UpdateShippingMethodCommandValidator : AbstractValidator<UpdateShippingMethodCommand>
{
    public UpdateShippingMethodCommandValidator()
    {
        RuleFor(command => command.ShippingMethodId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Provider).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Type).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.BasePriceAmount).GreaterThanOrEqualTo(0m);
    }
}
