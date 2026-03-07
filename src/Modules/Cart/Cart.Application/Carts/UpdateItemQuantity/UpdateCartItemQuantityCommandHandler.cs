using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Cart.Application.Carts.UpdateItemQuantity;

public sealed class UpdateCartItemQuantityCommandHandler(
    ICartRepository cartRepository,
    ICartUnitOfWork unitOfWork,
    ICustomerSessionCache customerSessionCache,
    IInventoryReservationService inventoryReservationService)
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

        var existingLine = cart.Lines.FirstOrDefault(line => line.VariantId == request.VariantId);
        if (existingLine is null)
        {
            return Result<Guid>.Failure(new Error("cart.item.not_found", "Cart item was not found."));
        }

        var existingQuantity = existingLine.Quantity;
        var reservationResult = await inventoryReservationService.SyncCartReservationAsync(
            request.CustomerId,
            existingLine.ProductId,
            request.VariantId,
            existingLine.Sku,
            request.Quantity,
            cancellationToken);
        if (reservationResult.IsFailure)
        {
            return Result<Guid>.Failure(reservationResult.Error);
        }

        var updateResult = cart.UpdateItemQuantity(request.VariantId, request.Quantity);
        if (updateResult.IsFailure)
        {
            await inventoryReservationService.SyncCartReservationAsync(
                request.CustomerId,
                existingLine.ProductId,
                request.VariantId,
                existingLine.Sku,
                existingQuantity,
                cancellationToken);
            return Result<Guid>.Failure(updateResult.Error);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await inventoryReservationService.SyncCartReservationAsync(
                request.CustomerId,
                existingLine.ProductId,
                request.VariantId,
                existingLine.Sku,
                existingQuantity,
                cancellationToken);
            throw;
        }

        await customerSessionCache.TouchCartSessionAsync(request.CustomerId, cancellationToken);
        return Result<Guid>.Success(cart.Id);
    }
}
