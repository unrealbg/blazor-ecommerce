using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;
using Customers.Domain.Customers;

namespace Customers.Application.Customers;

public sealed class AddAddressCommandHandler(
    ICustomerRepository customerRepository,
    ICustomersUnitOfWork unitOfWork)
    : ICommandHandler<AddAddressCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer is null)
        {
            return Result<Guid>.Failure(new Error("customers.not_found", "Customer profile was not found."));
        }

        var result = customer.AddAddress(ToAddressData(request.Address));
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static AddressData ToAddressData(AddressInput input)
    {
        return new AddressData(
            input.Label,
            input.FirstName,
            input.LastName,
            input.Company,
            input.Street1,
            input.Street2,
            input.City,
            input.PostalCode,
            input.CountryCode,
            input.Phone,
            input.IsDefaultShipping,
            input.IsDefaultBilling);
    }
}
