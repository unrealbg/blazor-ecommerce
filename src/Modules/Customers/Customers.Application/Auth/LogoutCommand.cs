using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Auth;

public sealed record LogoutCommand : ICommand<bool>;
