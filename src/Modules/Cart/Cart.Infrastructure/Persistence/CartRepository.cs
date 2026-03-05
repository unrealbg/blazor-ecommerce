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
        return dbContext.Carts
            .Include(cart => cart.Lines)
            .FirstOrDefaultAsync(cart => cart.CustomerId == customerId, cancellationToken);
    }
}
