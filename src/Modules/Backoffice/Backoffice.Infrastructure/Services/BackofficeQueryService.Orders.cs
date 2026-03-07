using Backoffice.Application.Backoffice;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;
using Payments.Domain.Payments;

namespace Backoffice.Infrastructure.Services;

internal sealed partial class BackofficeQueryService
{
    public async Task<BackofficeOrderPage> GetOrdersAsync(
        string? orderId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? status,
        string? paymentStatus,
        string? fulfillmentStatus,
        string? customerEmail,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var ordersQuery = ordersDbContext.Orders
            .AsNoTracking()
            .AsQueryable();

        if (Guid.TryParse(orderId, out var parsedOrderId))
        {
            ordersQuery = ordersQuery.Where(order => order.Id == parsedOrderId);
        }

        if (fromUtc is not null)
        {
            ordersQuery = ordersQuery.Where(order => order.PlacedAtUtc >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            ordersQuery = ordersQuery.Where(order => order.PlacedAtUtc <= toUtc.Value);
        }

        if (Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            ordersQuery = ordersQuery.Where(order => order.Status == parsedStatus);
        }

        if (Enum.TryParse<OrderFulfillmentStatus>(fulfillmentStatus, true, out var parsedFulfillmentStatus))
        {
            ordersQuery = ordersQuery.Where(order => order.FulfillmentStatus == parsedFulfillmentStatus);
        }

        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            var matchingCustomerIds = await ResolveCustomerKeysByEmailAsync(customerEmail, cancellationToken);
            if (matchingCustomerIds.Count == 0)
            {
                return EmptyOrderPage(normalizedPage, normalizedPageSize);
            }

            ordersQuery = ordersQuery.Where(order => matchingCustomerIds.Contains(order.CustomerId));
        }

        var orders = await ordersQuery
            .OrderByDescending(order => order.PlacedAtUtc)
            .ToListAsync(cancellationToken);

        var latestPayments = await GetLatestPaymentsByOrderAsync(
            orders.Select(order => order.Id),
            cancellationToken);

        if (Enum.TryParse<PaymentIntentStatus>(paymentStatus, true, out var parsedPaymentStatus))
        {
            orders = orders
                .Where(order =>
                    latestPayments.TryGetValue(order.Id, out var payment) &&
                    payment.Status == parsedPaymentStatus)
                .ToList();
        }

        var totalCount = orders.Count;
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        var pagedOrders = orders
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        var customerLookup = await GetCustomerLookupAsync(
            pagedOrders.Select(order => order.CustomerId),
            cancellationToken);

        var items = pagedOrders
            .Select(order =>
            {
                latestPayments.TryGetValue(order.Id, out var payment);
                customerLookup.TryGetValue(order.CustomerId, out var customer);
                return MapOrderListItem(order, customer, payment);
            })
            .ToArray();

        return new BackofficeOrderPage(
            normalizedPage,
            normalizedPageSize,
            totalCount,
            totalPages,
            items);
    }

    public async Task<BackofficeOrderDetailDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await ordersDbContext.Orders
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var customerLookup = await GetCustomerLookupAsync([order.CustomerId], cancellationToken);
        customerLookup.TryGetValue(order.CustomerId, out var customer);

        var payment = await paymentsDbContext.PaymentIntents
            .AsNoTracking()
            .Where(intent => intent.OrderId == order.Id)
            .OrderByDescending(intent => intent.UpdatedAtUtc)
            .ThenByDescending(intent => intent.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var paymentTransactions = payment is null
            ? []
            : await paymentsDbContext.PaymentTransactions
                .AsNoTracking()
                .Where(transaction => transaction.PaymentIntentId == payment.Id)
                .OrderByDescending(transaction => transaction.CreatedAtUtc)
                .ToListAsync(cancellationToken);

        var shipment = await shippingDbContext.Shipments
            .AsNoTracking()
            .Where(entity => entity.OrderId == order.Id)
            .OrderByDescending(entity => entity.UpdatedAtUtc)
            .ThenByDescending(entity => entity.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var shipmentEvents = shipment is null
            ? []
            : await shippingDbContext.ShipmentEvents
                .AsNoTracking()
                .Where(entity => entity.ShipmentId == shipment.Id)
                .OrderByDescending(entity => entity.OccurredAtUtc)
                .ToListAsync(cancellationToken);

        var notes = await GetOrderNotesAsync(order.Id, cancellationToken);
        var auditEntries = await GetOrderAuditEntriesAsync(order.Id, payment?.Id, shipment?.Id, cancellationToken);

        return new BackofficeOrderDetailDto(
            order.Id,
            order.CustomerId,
            customer?.Email,
            customer?.FullName,
            order.Status.ToString(),
            payment?.Status.ToString() ?? "Unknown",
            order.FulfillmentStatus.ToString(),
            order.SubtotalBeforeDiscount.Amount,
            order.Subtotal.Amount,
            order.LineDiscountTotal.Amount,
            order.CartDiscountTotal.Amount,
            order.ShippingPrice.Amount,
            order.ShippingDiscountTotal.Amount,
            order.Total.Amount,
            order.Total.Currency,
            order.ShippingMethodCode,
            order.ShippingMethodName,
            order.AppliedCouponsJson,
            order.AppliedPromotionsJson,
            order.PlacedAtUtc,
            MapAddress(order.ShippingAddress),
            MapAddress(order.BillingAddress),
            order.Lines.Select(MapOrderLine).ToArray(),
            payment is null ? null : MapPayment(payment, paymentTransactions),
            shipment is null ? null : MapShipment(shipment, shipmentEvents),
            notes,
            auditEntries);
    }
}
