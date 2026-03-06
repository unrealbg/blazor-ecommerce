using FluentValidation;

namespace Customers.Application.Auth;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.Token)
            .NotEmpty();
    }
}
