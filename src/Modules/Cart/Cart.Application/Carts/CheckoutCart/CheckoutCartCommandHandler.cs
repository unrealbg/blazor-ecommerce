using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;

namespace Cart.Application.Carts.CheckoutCart;

public sealed class CheckoutCartCommandHandler(
    ICartRepository cartRepository,
    ICartUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CheckoutCartCommand>
{
    public async Task<Result> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByIdAsync(request.CartId, cancellationToken);
        if (cart is null)
        {
            return Result.Failure(new Error("cart.not_found", "Cart was not found."));
        }

        var moneyResult = Money.Create(request.Currency, request.TotalAmount);
        if (moneyResult.IsFailure)
        {
            return Result.Failure(moneyResult.Error);
        }

        var checkoutResult = cart.Checkout(moneyResult.Value, clock.UtcNow);
        if (checkoutResult.IsFailure)
        {
            return checkoutResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
