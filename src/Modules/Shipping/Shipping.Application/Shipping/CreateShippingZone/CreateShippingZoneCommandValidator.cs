using FluentValidation;

namespace Shipping.Application.Shipping.CreateShippingZone;

public sealed class CreateShippingZoneCommandValidator : AbstractValidator<CreateShippingZoneCommand>
{
    public CreateShippingZoneCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.CountryCodes).NotEmpty();
    }
}
