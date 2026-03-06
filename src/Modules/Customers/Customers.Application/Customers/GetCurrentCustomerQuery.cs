using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Customers;

public sealed record GetCurrentCustomerQuery(Guid UserId) : IQuery<CustomerDto?>;
