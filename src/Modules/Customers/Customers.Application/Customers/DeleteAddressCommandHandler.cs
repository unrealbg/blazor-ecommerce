using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Customers.Application.Customers;

public sealed class DeleteAddressCommandHandler(
    ICustomerRepository customerRepository,
    ICustomersUnitOfWork unitOfWork)
    : ICommandHandler<DeleteAddressCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer is null)
        {
            return Result<bool>.Failure(new Error("customers.not_found", "Customer profile was not found."));
        }

        var removeResult = customer.DeleteAddress(request.AddressId);
        if (removeResult.IsFailure)
        {
            return Result<bool>.Failure(removeResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
