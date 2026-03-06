using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Customers;

public sealed class GetCurrentCustomerQueryHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCurrentCustomerQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(GetCurrentCustomerQuery request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return customer is null ? null : CustomerMapper.ToDto(customer);
    }
}
