using BuildingBlocks.Domain.Primitives;

namespace Customers.Domain.Customers.Events;

public sealed record CustomerAddressAdded(Guid CustomerId, Guid AddressId) : DomainEvent;
