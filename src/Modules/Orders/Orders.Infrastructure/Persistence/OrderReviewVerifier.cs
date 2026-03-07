using BuildingBlocks.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence;

internal sealed class OrderReviewVerifier(OrdersDbContext dbContext) : IOrderReviewVerifier
{
    public async Task<OrderReviewVerificationResult> VerifyPurchaseAsync(
        Guid customerId,
        Guid productId,
        Guid? variantId,
        CancellationToken cancellationToken)
    {
        if (customerId == Guid.Empty || productId == Guid.Empty)
        {
            return new OrderReviewVerificationResult(false, null);
        }

        var customerIdN = customerId.ToString("N");
        var customerIdD = customerId.ToString("D");

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .Where(order =>
                order.CustomerId == customerIdN ||
                order.CustomerId == customerIdD)
            .Where(order =>
                order.Status == OrderStatus.Paid ||
                order.Status == OrderStatus.PartiallyRefunded ||
                order.Status == OrderStatus.Refunded)
            .OrderByDescending(order => order.PaidAtUtc ?? order.PlacedAtUtc)
            .ToListAsync(cancellationToken);

        var matchedOrder = orders.FirstOrDefault(order => order.Lines.Any(line =>
            line.ProductId == productId &&
            (variantId is null || line.VariantId == variantId.Value)));

        return matchedOrder is null
            ? new OrderReviewVerificationResult(false, null)
            : new OrderReviewVerificationResult(true, matchedOrder.Id);
    }
}
