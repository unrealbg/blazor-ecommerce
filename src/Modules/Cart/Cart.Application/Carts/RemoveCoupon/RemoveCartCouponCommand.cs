using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.RemoveCoupon;

public sealed record RemoveCartCouponCommand(string CustomerId) : ICommand<Guid>;
