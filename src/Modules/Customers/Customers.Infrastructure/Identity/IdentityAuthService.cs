using BuildingBlocks.Domain.Results;
using Customers.Application.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Customers.Infrastructure.Identity;

internal sealed class IdentityAuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<IdentityAuthService> logger)
    : IIdentityAuthService
{
    public async Task<Result<IdentityRegisterResult>> RegisterAsync(
        IdentityRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            return Result<IdentityRegisterResult>.Failure(new Error(
                "auth.email.conflict",
                "An account with this email already exists."));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = request.EmailConfirmed,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errorDescription = string.Join("; ", createResult.Errors.Select(error => error.Description));
            return Result<IdentityRegisterResult>.Failure(new Error(
                "auth.register.failed",
                $"Unable to create account. {errorDescription}"));
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        return Result<IdentityRegisterResult>.Success(new IdentityRegisterResult(user.Id, token));
    }

    public async Task<Result<IdentityLoginResult>> LoginAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return Result<IdentityLoginResult>.Failure(new Error(
                "auth.login.invalid_credentials",
                "Invalid email or password."));
        }

        if (!user.IsActive())
        {
            return Result<IdentityLoginResult>.Failure(new Error(
                "auth.login.inactive_user",
                "User account is inactive."));
        }

        var signInResult = await signInManager.PasswordSignInAsync(
            user,
            password,
            rememberMe,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return Result<IdentityLoginResult>.Failure(new Error(
                "auth.login.invalid_credentials",
                "Invalid email or password."));
        }

        return Result<IdentityLoginResult>.Success(new IdentityLoginResult(user.Id, user.Email ?? normalizedEmail));
    }

    public Task LogoutAsync(CancellationToken cancellationToken)
    {
        return signInManager.SignOutAsync();
    }

    public async Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return Result.Success();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        logger.LogInformation("Password reset token generated for user {UserId}: {Token}", user.Id, token);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return Result.Failure(new Error("auth.reset_password.user_not_found", "User account was not found."));
        }

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var errorDescription = string.Join("; ", result.Errors.Select(error => error.Description));
            return Result.Failure(new Error("auth.reset_password.failed", errorDescription));
        }

        return Result.Success();
    }

    public async Task<Result> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(new Error("auth.verify_email.user_not_found", "User account was not found."));
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            var errorDescription = string.Join("; ", result.Errors.Select(error => error.Description));
            return Result.Failure(new Error("auth.verify_email.failed", errorDescription));
        }

        return Result.Success();
    }
}
