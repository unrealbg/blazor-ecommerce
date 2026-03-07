using BuildingBlocks.Domain.Primitives;

namespace Catalog.Domain.Products.Events;

public sealed record ProductArchived(Guid ProductId) : DomainEvent;
