using Cart.Domain.Carts;
using CartAggregate = Cart.Domain.Carts.Cart;

namespace Cart.Application.Carts;

public interface ICartRepository
{
    Task AddAsync(CartAggregate cart, CancellationToken cancellationToken);

    Task<CartAggregate?> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken);
}
