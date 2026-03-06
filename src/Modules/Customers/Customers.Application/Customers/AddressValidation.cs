using FluentValidation;

namespace Customers.Application.Customers;

internal static class AddressValidation
{
    public static void ApplyRules<T>(IRuleBuilderInitial<T, AddressInput> ruleBuilder)
    {
        ruleBuilder.ChildRules(address =>
        {
            address.RuleFor(item => item.Label)
                .NotEmpty()
                .MaximumLength(80);

            address.RuleFor(item => item.FirstName)
                .NotEmpty()
                .MaximumLength(120);

            address.RuleFor(item => item.LastName)
                .NotEmpty()
                .MaximumLength(120);

            address.RuleFor(item => item.Company)
                .MaximumLength(180);

            address.RuleFor(item => item.Street1)
                .NotEmpty()
                .MaximumLength(200);

            address.RuleFor(item => item.Street2)
                .MaximumLength(200);

            address.RuleFor(item => item.City)
                .NotEmpty()
                .MaximumLength(120);

            address.RuleFor(item => item.PostalCode)
                .NotEmpty()
                .MaximumLength(40);

            address.RuleFor(item => item.CountryCode)
                .NotEmpty()
                .Length(2);

            address.RuleFor(item => item.Phone)
                .MaximumLength(64);
        });
    }
}
