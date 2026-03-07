using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Cart.Domain.Carts;
using CartAggregate = Cart.Domain.Carts.Cart;

namespace Cart.Application.Carts.AddItem;

public sealed class AddItemToCartCommandHandler(
    ICartRepository cartRepository,
    ICartUnitOfWork unitOfWork,
    IProductCatalogReader productCatalogReader,
    ICustomerSessionCache customerSessionCache,
    IInventoryReservationService inventoryReservationService)
    : ICommandHandler<AddItemToCartCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        var product = await productCatalogReader.GetByVariantIdAsync(request.VariantId, cancellationToken);
        if (product is null || !product.IsActive || product.Id != request.ProductId)
        {
            return Result<Guid>.Failure(new Error("cart.product.not_found", "Product variant was not found."));
        }

        var variant = product.Variants.FirstOrDefault(item => item.Id == request.VariantId && item.IsActive);
        if (variant is null)
        {
            return Result<Guid>.Failure(new Error("cart.variant.not_found", "Product variant was not found."));
        }

        var moneyResult = Money.Create(variant.Currency, variant.Amount);
        if (moneyResult.IsFailure)
        {
            return Result<Guid>.Failure(moneyResult.Error);
        }

        var cart = await cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        var isNewCart = cart is null;
        var existingQuantity = cart?.Lines.FirstOrDefault(line => line.VariantId == request.VariantId)?.Quantity ?? 0;
        var desiredQuantity = existingQuantity + request.Quantity;

        if (isNewCart)
        {
            var createCartResult = CartAggregate.Create(request.CustomerId);
            if (createCartResult.IsFailure)
            {
                return Result<Guid>.Failure(createCartResult.Error);
            }

            cart = createCartResult.Value;
            await cartRepository.AddAsync(cart, cancellationToken);
        }

        var reservationResult = await inventoryReservationService.SyncCartReservationAsync(
            request.CustomerId,
            request.ProductId,
            request.VariantId,
            variant.Sku,
            desiredQuantity,
            cancellationToken);
        if (reservationResult.IsFailure)
        {
            return Result<Guid>.Failure(reservationResult.Error);
        }

        var selectedOptionsJson = variant.SelectedOptions.Count == 0
            ? null
            : System.Text.Json.JsonSerializer.Serialize(
                variant.SelectedOptions.Select(option => new { option.OptionName, option.Value }));

        var addItemResult = cart!.AddItem(
            request.ProductId,
            request.VariantId,
            variant.Sku,
            product.Name,
            variant.Name,
            selectedOptionsJson,
            variant.ImageUrl ?? product.ImageUrl,
            moneyResult.Value,
            request.Quantity);
        if (addItemResult.IsFailure)
        {
            await inventoryReservationService.SyncCartReservationAsync(
                request.CustomerId,
                request.ProductId,
                request.VariantId,
                variant.Sku,
                existingQuantity,
                cancellationToken);
            return Result<Guid>.Failure(addItemResult.Error);
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
                request.VariantId,
                variant.Sku,
                existingQuantity,
                cancellationToken);
            throw;
        }

        await customerSessionCache.TouchCartSessionAsync(request.CustomerId, cancellationToken);
        return Result<Guid>.Success(cart.Id);
    }
}
