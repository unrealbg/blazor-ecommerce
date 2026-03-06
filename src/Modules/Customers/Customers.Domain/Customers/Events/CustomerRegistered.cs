using BuildingBlocks.Domain.Primitives;

namespace Customers.Domain.Customers.Events;

public sealed record CustomerRegistered(Guid CustomerId, Guid? UserId, string Email) : DomainEvent;
