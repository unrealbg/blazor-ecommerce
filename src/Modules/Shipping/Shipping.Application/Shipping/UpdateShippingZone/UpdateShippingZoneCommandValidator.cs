using FluentValidation;

namespace Shipping.Application.Shipping.UpdateShippingZone;

public sealed class UpdateShippingZoneCommandValidator : AbstractValidator<UpdateShippingZoneCommand>
{
    public UpdateShippingZoneCommandValidator()
    {
        RuleFor(command => command.ShippingZoneId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.CountryCodes).NotEmpty();
    }
}
