using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Cart.Application.Carts.GetCart;

public sealed class GetCartQueryHandler(
    ICartRepository cartRepository,
    ICustomerSessionCache customerSessionCache,
    IInventoryReservationService inventoryReservationService)
    : IQueryHandler<GetCartQuery, CartDto?>
{
    public async Task<CartDto?> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        await customerSessionCache.TouchCartSessionAsync(request.CustomerId, cancellationToken);

        var cart = await cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (cart is null)
        {
            return null;
        }

        var messages = new List<string>();
        var validationResult = await inventoryReservationService.ValidateCartReservationsAsync(
            request.CustomerId,
            cart.Lines.Select(line => new InventoryCartLineRequest(line.ProductId, line.VariantId, line.Sku, line.Quantity)).ToList(),
            cancellationToken);

        if (validationResult.IsFailure)
        {
            messages.Add(validationResult.Error.Message);
        }
        else if (!validationResult.Value.IsValid)
        {
            messages.Add("Some items in your cart were updated due to stock availability.");
        }

        return new CartDto(
            cart.Id,
            cart.CustomerId,
            cart.Lines
                .Select(line => new CartLineDto(
                    line.ProductId,
                    line.VariantId,
                    line.Sku,
                    line.ProductName,
                    line.VariantName,
                    line.SelectedOptionsJson,
                    line.ImageUrl,
                    line.UnitPrice.Currency,
                    line.UnitPrice.Amount,
                    line.Quantity))
                .ToList(),
            messages);
    }
}
