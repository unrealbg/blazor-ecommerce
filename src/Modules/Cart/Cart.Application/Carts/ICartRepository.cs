using Cart.Domain.Carts;

namespace Cart.Application.Carts;

public interface ICartRepository
{
    Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken);

    Task<ShoppingCart?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken);
}
