using BuildingBlocks.Application.Abstractions;
namespace Customers.Application.Customers;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber) : ICommand<bool>;
