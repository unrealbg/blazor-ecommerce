using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Auth;

public sealed record VerifyEmailCommand(Guid UserId, string Token) : ICommand<bool>;
