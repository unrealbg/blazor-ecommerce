using FluentValidation;

namespace Customers.Application.Customers;

public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.AddressId)
            .NotEqual(Guid.Empty);

        AddressValidation.ApplyRules(RuleFor(command => command.Address));
    }
}
