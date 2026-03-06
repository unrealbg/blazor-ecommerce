using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.GetCart;

public sealed class GetCartQueryHandler(
    ICartRepository cartRepository,
    ICustomerSessionCache customerSessionCache)
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

        return new CartDto(
            cart.Id,
            cart.CustomerId,
            cart.Lines
                .Select(line => new CartLineDto(
                    line.ProductId,
                    line.ProductName,
                    line.UnitPrice.Currency,
                    line.UnitPrice.Amount,
                    line.Quantity))
                .ToList());
    }
}
