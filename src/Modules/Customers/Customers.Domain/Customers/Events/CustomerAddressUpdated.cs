using BuildingBlocks.Domain.Primitives;

namespace Customers.Domain.Customers.Events;

public sealed record CustomerAddressUpdated(Guid CustomerId, Guid AddressId) : DomainEvent;
