using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;
using Customers.Application.Customers;

namespace Customers.Application.Auth;

public sealed class VerifyEmailCommandHandler(
    IIdentityAuthService identityAuthService,
    ICustomerRepository customerRepository,
    ICustomersUnitOfWork unitOfWork)
    : ICommandHandler<VerifyEmailCommand, bool>
{
    public async Task<Result<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var verifyResult = await identityAuthService.VerifyEmailAsync(request.UserId, request.Token, cancellationToken);
        if (verifyResult.IsFailure)
        {
            return Result<bool>.Failure(verifyResult.Error);
        }

        var customer = await customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer is not null)
        {
            var updateResult = customer.VerifyEmail();
            if (updateResult.IsFailure)
            {
                return Result<bool>.Failure(updateResult.Error);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}
