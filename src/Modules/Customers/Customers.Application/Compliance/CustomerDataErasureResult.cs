namespace Customers.Application.Compliance;

public sealed record CustomerDataErasureResult(
    Guid CustomerId,
    Guid? UserId,
    bool IdentityUserUpdated,
    DateTime ErasedAtUtc,
    string ReplacementEmail);