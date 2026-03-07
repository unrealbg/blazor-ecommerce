using System.Text.Json;
using BuildingBlocks.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Orders.Infrastructure.Persistence;

internal sealed class OrderPricingReader(OrdersDbContext dbContext) : IOrderPricingReader
{
    public async Task<OrderPricingSnapshot?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(entity => entity.Lines)
            .SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var appliedDiscounts = DeserializeDiscounts(order.AppliedPromotionsJson);

        return new OrderPricingSnapshot(
            order.Id,
            order.CustomerId,
            order.Total.Currency,
            order.Total.Amount,
            order.Status.ToString(),
            order.Lines
                .Select(line => new OrderPricingLineSnapshot(
                    line.ProductId,
                    line.VariantId,
                    line.Sku,
                    line.BaseUnitAmount,
                    line.UnitPrice.Amount,
                    line.DiscountTotalAmount,
                    line.Quantity,
                    DeserializeDiscounts(line.AppliedDiscountsJson)))
                .ToList(),
            appliedDiscounts);
    }

    private static IReadOnlyCollection<PricingDiscountApplication> DeserializeDiscounts(string? payload)
    {
        return string.IsNullOrWhiteSpace(payload)
            ? []
            : JsonSerializer.Deserialize<List<PricingDiscountApplication>>(payload) ?? [];
    }
}
