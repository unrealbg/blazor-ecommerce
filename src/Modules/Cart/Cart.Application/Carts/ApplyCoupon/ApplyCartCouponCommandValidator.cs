using FluentValidation;

namespace Cart.Application.Carts.ApplyCoupon;

internal sealed class ApplyCartCouponCommandValidator : AbstractValidator<ApplyCartCouponCommand>
{
    public ApplyCartCouponCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.CouponCode).NotEmpty().MaximumLength(64);
    }
}
