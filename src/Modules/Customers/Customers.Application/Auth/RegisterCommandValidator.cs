using FluentValidation;

namespace Customers.Application.Auth;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);

        RuleFor(command => command.FirstName)
            .MaximumLength(120);

        RuleFor(command => command.LastName)
            .MaximumLength(120);

        RuleFor(command => command.PhoneNumber)
            .MaximumLength(64);
    }
}
