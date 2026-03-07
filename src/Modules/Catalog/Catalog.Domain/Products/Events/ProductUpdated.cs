using BuildingBlocks.Domain.Primitives;

namespace Catalog.Domain.Products.Events;

public sealed record ProductUpdated(Guid ProductId) : DomainEvent;
