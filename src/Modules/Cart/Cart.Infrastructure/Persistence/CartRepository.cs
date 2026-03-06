using Cart.Application.Carts;
using Microsoft.EntityFrameworkCore;
using CartAggregate = Cart.Domain.Carts.Cart;

namespace Cart.Infrastructure.Persistence;

internal sealed class CartRepository(CartDbContext dbContext) : ICartRepository
{
    public Task AddAsync(CartAggregate cart, CancellationToken cancellationToken)
    {
        return dbContext.Carts.AddAsync(cart, cancellationToken).AsTask();
    }

    public Task<CartAggregate?> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken)
    {
        return GetByCustomerIdInternalAsync(customerId, cancellationToken);
    }

    private async Task<CartAggregate?> GetByCustomerIdInternalAsync(string customerId, CancellationToken cancellationToken)
    {
        var cart = await dbContext.Carts
            .FirstOrDefaultAsync(item => item.CustomerId == customerId, cancellationToken);
        if (cart is null)
        {
            return null;
        }

        await dbContext.Entry(cart).Collection(item => item.Lines).LoadAsync(cancellationToken);
        return cart;
    }
}
