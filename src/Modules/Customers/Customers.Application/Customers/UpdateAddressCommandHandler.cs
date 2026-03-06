using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;
using Customers.Domain.Customers;

namespace Customers.Application.Customers;

public sealed class UpdateAddressCommandHandler(
    ICustomerRepository customerRepository,
    ICustomersUnitOfWork unitOfWork)
    : ICommandHandler<UpdateAddressCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer is null)
        {
            return Result<bool>.Failure(new Error("customers.not_found", "Customer profile was not found."));
        }

        var updateResult = customer.UpdateAddress(request.AddressId, ToAddressData(request.Address));
        if (updateResult.IsFailure)
        {
            return Result<bool>.Failure(updateResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
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
