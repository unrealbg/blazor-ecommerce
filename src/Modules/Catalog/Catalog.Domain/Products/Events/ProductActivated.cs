using BuildingBlocks.Domain.Primitives;

namespace Catalog.Domain.Products.Events;

public sealed record ProductActivated(Guid ProductId) : DomainEvent;
