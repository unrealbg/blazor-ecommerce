using BuildingBlocks.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure.Orders;

internal sealed class OrderExportReader(OrdersDbContext dbContext) : ICustomerOrderExportReader
{
    public async Task<IReadOnlyCollection<CustomerOrderExportRecord>> ListByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var keys = new[] { customerId.ToString("D"), customerId.ToString("N") };

        return await dbContext.Orders
            .AsNoTracking()
            .Where(order => keys.Contains(order.CustomerId))
            .OrderByDescending(order => order.PlacedAtUtc)
            .Select(order => new CustomerOrderExportRecord(
                order.Id,
                order.Status.ToString(),
                order.LastPaymentIntentId == null ? "Unknown" : "Tracked",
                order.FulfillmentStatus.ToString(),
                order.Total.Amount,
                order.Total.Currency,
                order.PlacedAtUtc,
                order.ShippingMethodName))
            .ToListAsync(cancellationToken);
    }
}