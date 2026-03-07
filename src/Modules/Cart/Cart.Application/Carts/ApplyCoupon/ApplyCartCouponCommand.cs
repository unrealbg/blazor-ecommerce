using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.ApplyCoupon;

public sealed record ApplyCartCouponCommand(string CustomerId, string CouponCode) : ICommand<Guid>;
