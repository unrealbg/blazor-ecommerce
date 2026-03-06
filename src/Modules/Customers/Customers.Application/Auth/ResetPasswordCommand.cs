using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Auth;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : ICommand<bool>;
