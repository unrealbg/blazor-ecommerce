using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Cart.Application.Carts.GetCart;

public sealed class GetCartQueryHandler(
    ICartRepository cartRepository,
    ICustomerSessionCache customerSessionCache,
    IInventoryReservationService inventoryReservationService,
    ICartPricingService cartPricingService)
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

        var pricingResult = await cartPricingService.PriceAsync(
            new CartPricingRequest(
                cart.CustomerId,
                IsAuthenticated: false,
                cart.Lines.Select(line => new CartPricingLineRequest(line.ProductId, line.VariantId, line.Quantity)).ToList(),
                cart.AppliedCouponCode,
                Shipping: null),
            cancellationToken);

        if (pricingResult.IsFailure)
        {
            messages.Add(pricingResult.Error.Message);

            return new CartDto(
                cart.Id,
                cart.CustomerId,
                cart.AppliedCouponCode,
                cart.Lines.First().UnitPrice.Currency,
                cart.Lines.Sum(line => line.UnitPrice.Amount * line.Quantity),
                cart.Lines.Sum(line => line.UnitPrice.Amount * line.Quantity),
                0m,
                0m,
                cart.Lines.Sum(line => line.UnitPrice.Amount * line.Quantity),
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
                        null,
                        line.UnitPrice.Amount,
                        line.UnitPrice.Amount * line.Quantity,
                        0m,
                        line.Quantity))
                    .ToList(),
                [],
                messages);
        }

        messages.AddRange(pricingResult.Value.Messages);
        var linesByVariantId = cart.Lines.ToDictionary(line => line.VariantId);

        return new CartDto(
            cart.Id,
            cart.CustomerId,
            pricingResult.Value.AppliedCouponCode,
            pricingResult.Value.Currency,
            pricingResult.Value.SubtotalBeforeDiscountAmount,
            pricingResult.Value.SubtotalAmount,
            pricingResult.Value.LineDiscountTotalAmount,
            pricingResult.Value.CartDiscountTotalAmount,
            pricingResult.Value.GrandTotalAmount,
            pricingResult.Value.Lines
                .Select(line =>
                {
                    var cartLine = linesByVariantId[line.VariantId];
                    return new CartLineDto(
                        cartLine.ProductId,
                        cartLine.VariantId,
                        cartLine.Sku,
                        cartLine.ProductName,
                        cartLine.VariantName,
                        cartLine.SelectedOptionsJson,
                        cartLine.ImageUrl,
                        line.Currency,
                        line.BaseUnitPriceAmount,
                        line.CompareAtUnitPriceAmount,
                        line.FinalUnitPriceAmount,
                        line.LineTotalAmount,
                        line.DiscountTotalAmount,
                        cartLine.Quantity);
                })
                .ToList(),
            pricingResult.Value.AppliedDiscounts,
            messages);
    }
}
