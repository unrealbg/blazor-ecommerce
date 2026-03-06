using FluentValidation;

namespace Shipping.Application.Shipping.CreateShippingMethod;

public sealed class CreateShippingMethodCommandValidator : AbstractValidator<CreateShippingMethodCommand>
{
    public CreateShippingMethodCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Provider).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Type).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.BasePriceAmount).GreaterThanOrEqualTo(0m);
    }
}
