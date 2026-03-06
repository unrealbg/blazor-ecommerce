namespace Customers.Application.Auth;

public sealed record AuthResponseDto(Guid UserId, Guid CustomerId, string Email);
