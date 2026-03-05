namespace Cart.Application.Carts;

public interface ICartUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
