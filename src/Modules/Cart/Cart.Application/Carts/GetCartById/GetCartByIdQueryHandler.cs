using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.GetCartById;

public sealed class GetCartByIdQueryHandler(ICartRepository cartRepository)
    : IQueryHandler<GetCartByIdQuery, CartDto?>
{
    public async Task<CartDto?> Handle(GetCartByIdQuery request, CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByIdAsync(request.CartId, cancellationToken);
        if (cart is null)
        {
            return null;
        }

        return new CartDto(
            cart.Id,
            cart.CustomerId,
            cart.Status.ToString(),
            cart.CreatedOnUtc,
            cart.CheckedOutOnUtc,
            cart.CheckoutTotal?.Currency,
            cart.CheckoutTotal?.Amount);
    }
}
