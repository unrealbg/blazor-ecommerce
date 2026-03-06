using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;
using Customers.Application.Customers;
using Customers.Domain.Customers;
using Microsoft.Extensions.Logging;

namespace Customers.Application.Auth;

public sealed class RegisterCommandHandler(
    IIdentityAuthService identityAuthService,
    ICustomerRepository customerRepository,
    ICustomersUnitOfWork unitOfWork,
    ILogger<RegisterCommandHandler> logger)
    : ICommandHandler<RegisterCommand, AuthResponseDto>
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existing = await customerRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            return Result<AuthResponseDto>.Failure(new Error(
                "customers.email.conflict",
                "A customer with this email already exists."));
        }

        var registerResult = await identityAuthService.RegisterAsync(
            new IdentityRegisterRequest(request.Email, request.Password, EmailConfirmed: false),
            cancellationToken);

        if (registerResult.IsFailure)
        {
            return Result<AuthResponseDto>.Failure(registerResult.Error);
        }

        var customerResult = Customer.CreateRegistered(
            request.Email,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            registerResult.Value.UserId);

        if (customerResult.IsFailure)
        {
            return Result<AuthResponseDto>.Failure(customerResult.Error);
        }

        await customerRepository.AddAsync(customerResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Email verification token generated for user {UserId}: {Token}",
            registerResult.Value.UserId,
            registerResult.Value.EmailVerificationToken);

        return Result<AuthResponseDto>.Success(
            new AuthResponseDto(
                registerResult.Value.UserId,
                customerResult.Value.Id,
                customerResult.Value.Email));
    }
}
