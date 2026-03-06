namespace Customers.Application.Auth;

public sealed record IdentityRegisterResult(Guid UserId, string EmailVerificationToken);
