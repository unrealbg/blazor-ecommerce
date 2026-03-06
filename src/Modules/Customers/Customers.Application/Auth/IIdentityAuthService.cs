using BuildingBlocks.Domain.Results;

namespace Customers.Application.Auth;

public interface IIdentityAuthService
{
    Task<Result<IdentityRegisterResult>> RegisterAsync(
        IdentityRegisterRequest request,
        CancellationToken cancellationToken);

    Task<Result<IdentityLoginResult>> LoginAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken);

    Task LogoutAsync(CancellationToken cancellationToken);

    Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken);

    Task<Result> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken);
}
