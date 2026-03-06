using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Auth;

public sealed record ForgotPasswordCommand(string Email) : ICommand<bool>;
