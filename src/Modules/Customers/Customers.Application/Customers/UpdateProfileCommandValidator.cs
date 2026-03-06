using FluentValidation;

namespace Customers.Application.Customers;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.FirstName)
            .MaximumLength(120);

        RuleFor(command => command.LastName)
            .MaximumLength(120);

        RuleFor(command => command.PhoneNumber)
            .MaximumLength(64);
    }
}
