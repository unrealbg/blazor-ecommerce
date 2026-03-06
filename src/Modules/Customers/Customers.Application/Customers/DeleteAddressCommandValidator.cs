using FluentValidation;

namespace Customers.Application.Customers;

public sealed class DeleteAddressCommandValidator : AbstractValidator<DeleteAddressCommand>
{
    public DeleteAddressCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.AddressId)
            .NotEqual(Guid.Empty);
    }
}
