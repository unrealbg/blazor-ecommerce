namespace Orders.Application.Orders;

public interface IOrderAuditRepository
{
    Task AddAsync(
        Guid eventId,
        Guid orderId,
        string customerId,
        string currency,
        decimal totalAmount,
        DateTime loggedOnUtc,
        CancellationToken cancellationToken);
}
