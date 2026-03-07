using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Cart.Application.Carts.RemoveCoupon;

public sealed class RemoveCartCouponCommandHandler(
    ICartRepository cartRepository,
    ICartUnitOfWork unitOfWork)
    : ICommandHandler<RemoveCartCouponCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RemoveCartCouponCommand request, CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (cart is null)
        {
            return Result<Guid>.Failure(new Error("cart.not_found", "Cart was not found."));
        }

        cart.RemoveCoupon();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(cart.Id);
    }
}
