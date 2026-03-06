using BuildingBlocks.Domain.Results;
using Customers.Application.Auth;
using Customers.Application.Customers;
using Customers.Domain.Customers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Customers.Tests;

public sealed class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_RegisterCustomer_WhenEmailIsFree()
    {
        var repository = new StubCustomerRepository();
        var handler = new RegisterCommandHandler(
            new StubIdentityAuthService(
                new IdentityRegisterResult(
                    Guid.Parse("f9fc85a7-b874-4bd4-9120-f3f557ff0c90"),
                    "verify-token")),
            repository,
            new StubCustomersUnitOfWork(),
            NullLogger<RegisterCommandHandler>.Instance);

        var result = await handler.Handle(
            new RegisterCommand("new@example.com", "Password1", "John", "Doe", "+359888000000"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedCustomer);
        Assert.Equal("new@example.com", repository.AddedCustomer!.Email);
    }

    [Fact]
    public async Task Handle_Should_RejectDuplicateEmail()
    {
        var existingCustomer = Customer.CreateGuest("existing@example.com", null, null, null).Value;
        var repository = new StubCustomerRepository
        {
            CustomerByNormalizedEmail = existingCustomer,
        };

        var handler = new RegisterCommandHandler(
            new StubIdentityAuthService(
                new IdentityRegisterResult(Guid.NewGuid(), "verify-token")),
            repository,
            new StubCustomersUnitOfWork(),
            NullLogger<RegisterCommandHandler>.Instance);

        var result = await handler.Handle(
            new RegisterCommand("existing@example.com", "Password1", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("customers.email.conflict", result.Error.Code);
        Assert.Null(repository.AddedCustomer);
    }

    private sealed class StubIdentityAuthService(IdentityRegisterResult registerResult) : IIdentityAuthService
    {
        public Task<Result<IdentityRegisterResult>> RegisterAsync(
            IdentityRegisterRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<IdentityRegisterResult>.Success(registerResult));
        }

        public Task<Result<IdentityLoginResult>> LoginAsync(
            string email,
            string password,
            bool rememberMe,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task LogoutAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result> ResetPasswordAsync(
            string email,
            string token,
            string newPassword,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubCustomerRepository : ICustomerRepository
    {
        public Customer? CustomerByNormalizedEmail { get; set; }

        public Customer? AddedCustomer { get; private set; }

        public Task AddAsync(Customer customer, CancellationToken cancellationToken)
        {
            AddedCustomer = customer;
            return Task.CompletedTask;
        }

        public Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Customer?>(null);
        }

        public Task<Customer?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult(CustomerByNormalizedEmail);
        }

        public Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Customer?>(null);
        }
    }

    private sealed class StubCustomersUnitOfWork : ICustomersUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
