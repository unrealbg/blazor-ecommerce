namespace BuildingBlocks.Application.Contracts;

public sealed record CustomerCheckoutProfile(
    Guid CustomerId,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber);
