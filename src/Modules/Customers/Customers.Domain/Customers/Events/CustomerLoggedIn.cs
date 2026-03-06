using BuildingBlocks.Domain.Primitives;

namespace Customers.Domain.Customers.Events;

public sealed record CustomerLoggedIn(Guid CustomerId, Guid UserId) : DomainEvent;
