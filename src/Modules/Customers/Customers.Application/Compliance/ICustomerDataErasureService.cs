namespace Customers.Application.Compliance;

public interface ICustomerDataErasureService
{
    Task<CustomerDataErasureResult?> EraseByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}