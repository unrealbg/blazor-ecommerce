using Backoffice.Application.Backoffice;
using Microsoft.EntityFrameworkCore;

namespace Backoffice.Infrastructure.Services;

internal sealed partial class BackofficeQueryService
{
    public async Task<BackofficeCustomerPage> GetCustomersAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var customersQuery = customersDbContext.Customers
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim().ToLowerInvariant();
            var parsedId = Guid.TryParse(query, out var resolvedId) ? resolvedId : Guid.Empty;

            customersQuery = customersQuery.Where(customer =>
                customer.Id == parsedId ||
                customer.UserId == parsedId ||
                customer.Email.ToLower().Contains(normalizedQuery) ||
                (customer.FirstName != null && customer.FirstName.ToLower().Contains(normalizedQuery)) ||
                (customer.LastName != null && customer.LastName.ToLower().Contains(normalizedQuery)));
        }

        var totalCount = await customersQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        var customers = await customersQuery
            .OrderByDescending(customer => customer.CreatedAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var orderAggregates = await GetCustomerOrderAggregatesAsync(customers.Select(customer => customer.Id), cancellationToken);

        var items = customers
            .Select(customer =>
            {
                orderAggregates.TryGetValue(customer.Id, out var aggregate);
                aggregate ??= new CustomerOrderAggregate(0, null);

                return new BackofficeCustomerSummaryDto(
                    customer.Id,
                    customer.Email,
                    BuildFullName(customer.FirstName, customer.LastName),
                    customer.PhoneNumber,
                    customer.IsEmailVerified,
                    customer.IsActive,
                    customer.CreatedAtUtc,
                    aggregate.OrderCount,
                    aggregate.LastOrderAtUtc);
            })
            .ToArray();

        return new BackofficeCustomerPage(
            normalizedPage,
            normalizedPageSize,
            totalCount,
            totalPages,
            items);
    }

    public async Task<BackofficeCustomerDetailDto?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await customersDbContext.Customers
            .AsNoTracking()
            .Include(entity => entity.Addresses)
            .SingleOrDefaultAsync(entity => entity.Id == customerId, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var customerKeys = BuildCustomerKeys(customer.Id);
        var orders = await ordersDbContext.Orders
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .Where(order => customerKeys.Contains(order.CustomerId))
            .OrderByDescending(order => order.PlacedAtUtc)
            .ToListAsync(cancellationToken);

        var latestPayments = await GetLatestPaymentsByOrderAsync(orders.Select(order => order.Id), cancellationToken);
        var lookup = new CustomerLookup(customer.Id, customer.Email, BuildFullName(customer.FirstName, customer.LastName));
        var orderItems = orders
            .Select(order =>
            {
                latestPayments.TryGetValue(order.Id, out var payment);
                return MapOrderListItem(order, lookup, payment);
            })
            .ToArray();

        var reviews = await reviewsDbContext.ProductReviews
            .AsNoTracking()
            .Where(review => review.CustomerId == customer.Id)
            .OrderByDescending(review => review.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        var questions = await reviewsDbContext.ProductQuestions
            .AsNoTracking()
            .Where(question => question.CustomerId == customer.Id)
            .OrderByDescending(question => question.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        var activity = BuildCustomerActivity(orderItems, reviews, questions);

        return new BackofficeCustomerDetailDto(
            customer.Id,
            customer.UserId,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.PhoneNumber,
            customer.IsEmailVerified,
            customer.IsActive,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc,
            customer.Addresses
                .OrderByDescending(address => address.IsDefaultShipping || address.IsDefaultBilling)
                .ThenBy(address => address.Label)
                .Select(address => MapCustomerAddress(address))
                .ToArray(),
            orderItems,
            activity);
    }
}
