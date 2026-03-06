using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Auth;

public sealed record LoginCommand(string Email, string Password, bool RememberMe) : ICommand<AuthResponseDto>;
