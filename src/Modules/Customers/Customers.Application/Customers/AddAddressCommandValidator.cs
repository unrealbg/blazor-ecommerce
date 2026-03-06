using FluentValidation;

namespace Customers.Application.Customers;

public sealed class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
    public AddAddressCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEqual(Guid.Empty);

        AddressValidation.ApplyRules(RuleFor(command => command.Address));
    }
}
