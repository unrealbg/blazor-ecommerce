using Backoffice.Application.Backoffice;
using Backoffice.Domain.Audit;
using Customers.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;
using Payments.Domain.Payments;
using Reviews.Domain.Questions;
using Reviews.Domain.Reviews;
using Shipping.Domain.Shipping;

namespace Backoffice.Infrastructure.Services;

internal sealed partial class BackofficeQueryService
{
    private async Task<Dictionary<Guid, PaymentIntent>> GetLatestPaymentsByOrderAsync(
        IEnumerable<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        var orderIdArray = orderIds.Distinct().ToArray();
        if (orderIdArray.Length == 0)
        {
            return [];
        }

        var payments = await paymentsDbContext.PaymentIntents
            .AsNoTracking()
            .Where(intent => orderIdArray.Contains(intent.OrderId))
            .OrderByDescending(intent => intent.UpdatedAtUtc)
            .ThenByDescending(intent => intent.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return payments
            .GroupBy(intent => intent.OrderId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private async Task<HashSet<string>> ResolveCustomerKeysByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var customerIds = await customersDbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Email.ToLower().Contains(normalizedEmail))
            .Select(customer => customer.Id)
            .ToListAsync(cancellationToken);

        return customerIds
            .SelectMany(BuildCustomerKeys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, CustomerLookup>> GetCustomerLookupAsync(
        IEnumerable<string> customerIds,
        CancellationToken cancellationToken)
    {
        var parsedCustomerIds = customerIds
            .Select(value => Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty)
            .Where(value => value != Guid.Empty)
            .Distinct()
            .ToArray();

        if (parsedCustomerIds.Length == 0)
        {
            return new Dictionary<string, CustomerLookup>(StringComparer.OrdinalIgnoreCase);
        }

        var customers = await customersDbContext.Customers
            .AsNoTracking()
            .Where(customer => parsedCustomerIds.Contains(customer.Id))
            .Select(customer => new
            {
                customer.Id,
                customer.Email,
                customer.FirstName,
                customer.LastName,
            })
            .ToListAsync(cancellationToken);

        var lookup = new Dictionary<string, CustomerLookup>(StringComparer.OrdinalIgnoreCase);
        foreach (var customer in customers)
        {
            var value = new CustomerLookup(
                customer.Id,
                customer.Email,
                BuildFullName(customer.FirstName, customer.LastName));

            foreach (var key in BuildCustomerKeys(customer.Id))
            {
                lookup[key] = value;
            }
        }

        return lookup;
    }

    private async Task<Dictionary<Guid, CustomerOrderAggregate>> GetCustomerOrderAggregatesAsync(
        IEnumerable<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        var ids = customerIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var keys = ids.SelectMany(BuildCustomerKeys).ToArray();
        var orders = await ordersDbContext.Orders
            .AsNoTracking()
            .Where(order => keys.Contains(order.CustomerId))
            .Select(order => new { order.CustomerId, order.PlacedAtUtc })
            .ToListAsync(cancellationToken);

        var aggregates = new Dictionary<Guid, CustomerOrderAggregate>();
        foreach (var customerId in ids)
        {
            var customerKeys = BuildCustomerKeys(customerId);
            var matchingOrders = orders.Where(order => customerKeys.Contains(order.CustomerId)).ToArray();
            aggregates[customerId] = new CustomerOrderAggregate(
                matchingOrders.Length,
                matchingOrders.Length == 0
                    ? null
                    : matchingOrders.Max(order => order.PlacedAtUtc));
        }

        return aggregates;
    }

    private static IReadOnlyCollection<BackofficeCustomerActivityItemDto> BuildCustomerActivity(
        IReadOnlyCollection<BackofficeOrderListItemDto> orders,
        IReadOnlyCollection<ProductReview> reviews,
        IReadOnlyCollection<ProductQuestion> questions)
    {
        var items = new List<BackofficeCustomerActivityItemDto>(orders.Count + reviews.Count + questions.Count);

        items.AddRange(orders.Select(order => new BackofficeCustomerActivityItemDto(
            "Order",
            $"Order {order.Id:D} is {order.Status}.",
            order.PlacedAtUtc,
            "Order",
            order.Id.ToString("D"))));

        items.AddRange(reviews.Select(review => new BackofficeCustomerActivityItemDto(
            "Review",
            $"Review rated {review.Rating}/5 is {review.Status}.",
            review.CreatedAtUtc,
            "Review",
            review.Id.ToString("D"))));

        items.AddRange(questions.Select(question => new BackofficeCustomerActivityItemDto(
            "Question",
            $"Question is {question.Status}: {TrimSummary(question.QuestionText)}",
            question.CreatedAtUtc,
            "Question",
            question.Id.ToString("D"))));

        return items
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(30)
            .ToArray();
    }

    private static BackofficeOrderListItemDto MapOrderListItem(
        Order order,
        CustomerLookup? customer,
        PaymentIntent? payment)
    {
        return new BackofficeOrderListItemDto(
            order.Id,
            order.CustomerId,
            customer?.Email,
            customer?.FullName,
            order.Status.ToString(),
            payment?.Status.ToString() ?? "Unknown",
            order.FulfillmentStatus.ToString(),
            order.Total.Amount,
            order.Total.Currency,
            order.ShippingMethodName,
            order.Lines.Count,
            order.PlacedAtUtc);
    }

    private static BackofficeOrderLineDto MapOrderLine(OrderLine line)
    {
        return new BackofficeOrderLineDto(
            line.ProductId,
            line.VariantId,
            line.ProductName,
            line.VariantName,
            line.Sku,
            line.UnitPrice.Currency,
            line.UnitPrice.Amount,
            line.DiscountTotalAmount,
            line.CompareAtPriceAmount,
            line.Quantity,
            line.SelectedOptionsJson);
    }

    private static BackofficeAddressDto MapAddress(OrderAddressSnapshot address)
    {
        return new BackofficeAddressDto(
            AddressId: null,
            address.FirstName,
            address.LastName,
            Company: null,
            Street1: address.Street,
            Street2: null,
            address.City,
            address.PostalCode,
            address.Country,
            address.Phone,
            IsDefaultShipping: false,
            IsDefaultBilling: false);
    }

    private static BackofficeCustomerAddressDto MapCustomerAddress(Address address)
    {
        return new BackofficeCustomerAddressDto(
            address.Id,
            address.Label,
            address.FirstName,
            address.LastName,
            address.Company,
            address.Street1,
            address.Street2,
            address.City,
            address.PostalCode,
            address.CountryCode,
            address.Phone,
            address.IsDefaultShipping,
            address.IsDefaultBilling,
            address.CreatedAtUtc,
            address.UpdatedAtUtc);
    }

    private static BackofficePaymentSummaryDto MapPayment(
        PaymentIntent payment,
        IReadOnlyCollection<PaymentTransaction> transactions)
    {
        return new BackofficePaymentSummaryDto(
            payment.Id,
            payment.Provider,
            payment.Status.ToString(),
            payment.Amount,
            payment.Currency,
            payment.ProviderPaymentIntentId,
            payment.FailureCode,
            payment.FailureMessage,
            payment.CreatedAtUtc,
            payment.UpdatedAtUtc,
            payment.CompletedAtUtc,
            transactions
                .Select(transaction => new BackofficePaymentTransactionDto(
                    transaction.Id,
                    transaction.Type.ToString(),
                    transaction.Amount,
                    transaction.Currency,
                    transaction.Status,
                    transaction.ProviderTransactionId,
                    transaction.RawReference,
                    transaction.CreatedAtUtc))
                .ToArray());
    }

    private static BackofficeShipmentSummaryDto MapShipment(
        Shipment shipment,
        IReadOnlyCollection<ShipmentEvent> events)
    {
        return new BackofficeShipmentSummaryDto(
            shipment.Id,
            shipment.Status.ToString(),
            shipment.CarrierName,
            shipment.CarrierServiceCode,
            shipment.TrackingNumber,
            shipment.TrackingUrl,
            shipment.ShippingPriceAmount,
            shipment.Currency,
            shipment.CreatedAtUtc,
            shipment.UpdatedAtUtc,
            shipment.ShippedAtUtc,
            shipment.DeliveredAtUtc,
            events
                .Select(@event => new BackofficeShipmentEventDto(
                    @event.Id,
                    @event.EventType.ToString(),
                    @event.Message,
                    @event.ExternalEventId,
                    @event.OccurredAtUtc,
                    @event.MetadataJson))
                .ToArray());
    }

    private static BackofficeAuditEntryDto MapAuditEntry(AuditEntry entry)
    {
        return new BackofficeAuditEntryDto(
            entry.Id,
            entry.OccurredAtUtc,
            entry.ActionType,
            entry.TargetType,
            entry.TargetId,
            entry.Summary,
            entry.MetadataJson,
            entry.ActorUserId,
            entry.ActorEmail,
            entry.ActorDisplayName,
            entry.IpAddress,
            entry.CorrelationId);
    }

    private static BackofficeOrderPage EmptyOrderPage(int page, int pageSize)
    {
        return new BackofficeOrderPage(page, pageSize, 0, 1, []);
    }

    private static string? BuildFullName(string? firstName, string? lastName)
    {
        var parts = new[] { firstName?.Trim(), lastName?.Trim() }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? null : string.Join(' ', parts);
    }

    private static IReadOnlyCollection<string> BuildCustomerKeys(Guid customerId)
    {
        return
        [
            customerId.ToString("D"),
            customerId.ToString("N"),
        ];
    }

    private static string TrimSummary(string value)
    {
        return value.Length <= 80 ? value : $"{value[..77]}...";
    }

    private sealed record CustomerLookup(Guid Id, string Email, string? FullName);

    private sealed record CustomerOrderAggregate(int OrderCount, DateTime? LastOrderAtUtc);
}
