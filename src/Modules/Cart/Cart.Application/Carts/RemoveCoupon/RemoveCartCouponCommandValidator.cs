using FluentValidation;

namespace Cart.Application.Carts.RemoveCoupon;

internal sealed class RemoveCartCouponCommandValidator : AbstractValidator<RemoveCartCouponCommand>
{
    public RemoveCartCouponCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
    }
}
