using FluentValidation;

namespace Shipping.Application.Shipping.CreateShipment;

public sealed class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.ShippingMethodCode).MaximumLength(64);
    }
}
