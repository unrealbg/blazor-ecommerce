using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Auth;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? PhoneNumber) : ICommand<AuthResponseDto>;
