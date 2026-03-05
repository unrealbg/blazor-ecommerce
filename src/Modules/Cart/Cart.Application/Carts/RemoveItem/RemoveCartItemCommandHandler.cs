using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Cart.Application.Carts.RemoveItem;

public sealed class RemoveCartItemCommandHandler(
    ICartRepository cartRepository,
    ICartUnitOfWork unitOfWork)
    : ICommandHandler<RemoveCartItemCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        RemoveCartItemCommand request,
        CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (cart is null)
        {
            return Result<Guid>.Failure(new Error("cart.not_found", "Cart was not found."));
        }

        var removeResult = cart.RemoveItem(request.ProductId);
        if (removeResult.IsFailure)
        {
            return Result<Guid>.Failure(removeResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(cart.Id);
    }
}
