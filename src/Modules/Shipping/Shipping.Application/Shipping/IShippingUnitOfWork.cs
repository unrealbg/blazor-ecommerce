namespace Shipping.Application.Shipping;

public interface IShippingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
