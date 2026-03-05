using Cart.Application.Carts;
using Cart.Domain.Carts;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Persistence;

internal sealed class CartRepository(CartDbContext dbContext) : ICartRepository
{
    public Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        return dbContext.Carts.AddAsync(cart, cancellationToken).AsTask();
    }

    public Task<ShoppingCart?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken)
    {
        return dbContext.Carts.FirstOrDefaultAsync(cart => cart.Id == cartId, cancellationToken);
    }
}
