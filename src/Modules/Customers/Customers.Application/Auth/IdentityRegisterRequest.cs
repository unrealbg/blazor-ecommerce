namespace Customers.Application.Auth;

public sealed record IdentityRegisterRequest(
    string Email,
    string Password,
    bool EmailConfirmed);
