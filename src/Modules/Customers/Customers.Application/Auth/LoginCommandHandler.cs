using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;
using Customers.Application.Customers;
using Customers.Domain.Customers;

namespace Customers.Application.Auth;

public sealed class LoginCommandHandler(
    IIdentityAuthService identityAuthService,
    ICustomerRepository customerRepository,
    ICustomersUnitOfWork unitOfWork)
    : ICommandHandler<LoginCommand, AuthResponseDto>
{
    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var loginResult = await identityAuthService.LoginAsync(
            request.Email,
            request.Password,
            request.RememberMe,
            cancellationToken);

        if (loginResult.IsFailure)
        {
            return Result<AuthResponseDto>.Failure(loginResult.Error);
        }

        var customer = await customerRepository.GetByUserIdAsync(loginResult.Value.UserId, cancellationToken);
        if (customer is null)
        {
            var createCustomerResult = Customer.CreateRegistered(
                loginResult.Value.Email,
                firstName: null,
                lastName: null,
                phoneNumber: null,
                loginResult.Value.UserId);
            if (createCustomerResult.IsFailure)
            {
                return Result<AuthResponseDto>.Failure(createCustomerResult.Error);
            }

            customer = createCustomerResult.Value;
            await customerRepository.AddAsync(customer, cancellationToken);
        }

        customer.RecordLogin(loginResult.Value.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResponseDto>.Success(
            new AuthResponseDto(loginResult.Value.UserId, customer.Id, customer.Email));
    }
}
