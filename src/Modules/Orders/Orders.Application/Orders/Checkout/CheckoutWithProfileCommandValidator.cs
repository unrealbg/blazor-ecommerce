using FluentValidation;

namespace Orders.Application.Orders.Checkout;

public sealed class CheckoutWithProfileCommandValidator : AbstractValidator<CheckoutWithProfileCommand>
{
    public CheckoutWithProfileCommandValidator()
    {
        RuleFor(command => command.CartSessionId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(command => command.ShippingAddress)
            .NotNull();

        RuleFor(command => command.BillingAddress)
            .NotNull();

        RuleFor(command => command.ShippingAddress.FirstName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.ShippingAddress.LastName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.ShippingAddress.Street)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.ShippingAddress.City)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.ShippingAddress.PostalCode)
            .NotEmpty()
            .MaximumLength(40);

        RuleFor(command => command.ShippingAddress.Country)
            .NotEmpty()
            .Length(2);

        RuleFor(command => command.BillingAddress.FirstName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.BillingAddress.LastName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.BillingAddress.Street)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.BillingAddress.City)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.BillingAddress.PostalCode)
            .NotEmpty()
            .MaximumLength(40);

        RuleFor(command => command.BillingAddress.Country)
            .NotEmpty()
            .Length(2);
    }
}
