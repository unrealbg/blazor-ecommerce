using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Cart.Application.Carts.UpdateItemQuantity;

public sealed class UpdateCartItemQuantityCommandHandler(
    ICartRepository cartRepository,
    ICartUnitOfWork unitOfWork,
    ICustomerSessionCache customerSessionCache)
    : ICommandHandler<UpdateCartItemQuantityCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        UpdateCartItemQuantityCommand request,
        CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (cart is null)
        {
            return Result<Guid>.Failure(new Error("cart.not_found", "Cart was not found."));
        }

        var updateResult = cart.UpdateItemQuantity(request.ProductId, request.Quantity);
        if (updateResult.IsFailure)
        {
            return Result<Guid>.Failure(updateResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await customerSessionCache.TouchCartSessionAsync(request.CustomerId, cancellationToken);
        return Result<Guid>.Success(cart.Id);
    }
}
