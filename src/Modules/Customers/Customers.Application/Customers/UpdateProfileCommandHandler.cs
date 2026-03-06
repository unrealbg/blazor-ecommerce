using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Customers.Application.Customers;

public sealed class UpdateProfileCommandHandler(
    ICustomerRepository customerRepository,
    ICustomersUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProfileCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer is null)
        {
            return Result<bool>.Failure(new Error("customers.not_found", "Customer profile was not found."));
        }

        var updateResult = customer.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);
        if (updateResult.IsFailure)
        {
            return Result<bool>.Failure(updateResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
