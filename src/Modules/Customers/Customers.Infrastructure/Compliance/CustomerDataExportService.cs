using BuildingBlocks.Application.Contracts;
using Customers.Application.Compliance;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Compliance;

internal sealed class CustomerDataExportService(
    CustomersDbContext customersDbContext,
    ICustomerOrderExportReader customerOrderExportReader,
    ICustomerReviewExportReader customerReviewExportReader)
    : ICustomerDataExportService
{
    public async Task<CustomerDataExportDto?> ExportByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var customer = await customersDbContext.Customers
            .AsNoTracking()
            .Include(entity => entity.Addresses)
            .SingleOrDefaultAsync(entity => entity.UserId == userId, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var orders = await customerOrderExportReader.ListByCustomerIdAsync(customer.Id, cancellationToken);
        var reviews = await customerReviewExportReader.ListReviewsByCustomerIdAsync(customer.Id, cancellationToken);
        var questions = await customerReviewExportReader.ListQuestionsByCustomerIdAsync(customer.Id, cancellationToken);

        return new CustomerDataExportDto(
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
                .OrderBy(address => address.Label)
                .Select(address => new CustomerAddressExportDto(
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
                    address.UpdatedAtUtc))
                .ToArray(),
            orders,
            reviews,
            questions);
    }
}