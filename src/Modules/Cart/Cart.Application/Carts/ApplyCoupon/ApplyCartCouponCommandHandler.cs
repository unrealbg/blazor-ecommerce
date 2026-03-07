using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;

namespace Cart.Application.Carts.ApplyCoupon;

public sealed class ApplyCartCouponCommandHandler(
    ICartRepository cartRepository,
    ICartUnitOfWork unitOfWork,
    ICartPricingService cartPricingService)
    : ICommandHandler<ApplyCartCouponCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ApplyCartCouponCommand request, CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (cart is null)
        {
            return Result<Guid>.Failure(new Error("cart.not_found", "Cart was not found."));
        }

        var validationResult = await cartPricingService.ValidateCouponAsync(
            new CouponValidationRequest(
                request.CouponCode,
                request.CustomerId,
                IsAuthenticated: false,
                cart.Lines.Select(line => new CartPricingLineRequest(line.ProductId, line.VariantId, line.Quantity)).ToList()),
            cancellationToken);

        if (validationResult.IsFailure)
        {
            return Result<Guid>.Failure(validationResult.Error);
        }

        var applyResult = cart.ApplyCoupon(validationResult.Value.Code);
        if (applyResult.IsFailure)
        {
            return Result<Guid>.Failure(applyResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(cart.Id);
    }
}
