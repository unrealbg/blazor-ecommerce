using BuildingBlocks.Domain.Primitives;

namespace Catalog.Domain.Products.Events;

public sealed record ProductSlugChanged(
    Guid ProductId,
    string PreviousSlug,
    string CurrentSlug) : DomainEvent;
