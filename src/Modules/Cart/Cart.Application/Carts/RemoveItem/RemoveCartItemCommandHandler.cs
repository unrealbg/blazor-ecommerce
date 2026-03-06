using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Cart.Application.Carts.RemoveItem;

public sealed class RemoveCartItemCommandHandler(
    ICartRepository cartRepository,
    ICartUnitOfWork unitOfWork,
    ICustomerSessionCache customerSessionCache,
    IInventoryReservationService inventoryReservationService)
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

        var existingLine = cart.Lines.FirstOrDefault(line => line.ProductId == request.ProductId);
        if (existingLine is null)
        {
            return Result<Guid>.Failure(new Error("cart.item.not_found", "Cart item was not found."));
        }

        var releaseResult = await inventoryReservationService.SyncCartReservationAsync(
            request.CustomerId,
            request.ProductId,
            sku: null,
            quantity: 0,
            cancellationToken);
        if (releaseResult.IsFailure)
        {
            return Result<Guid>.Failure(releaseResult.Error);
        }

        var removeResult = cart.RemoveItem(request.ProductId);
        if (removeResult.IsFailure)
        {
            await inventoryReservationService.SyncCartReservationAsync(
                request.CustomerId,
                request.ProductId,
                sku: null,
                existingLine.Quantity,
                cancellationToken);
            return Result<Guid>.Failure(removeResult.Error);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await inventoryReservationService.SyncCartReservationAsync(
                request.CustomerId,
                request.ProductId,
                sku: null,
                existingLine.Quantity,
                cancellationToken);
            throw;
        }

        await customerSessionCache.TouchCartSessionAsync(request.CustomerId, cancellationToken);
        return Result<Guid>.Success(cart.Id);
    }
}
