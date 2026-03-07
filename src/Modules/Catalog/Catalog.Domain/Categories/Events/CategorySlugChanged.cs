using BuildingBlocks.Domain.Primitives;

namespace Catalog.Domain.Categories.Events;

public sealed record CategorySlugChanged(
    Guid CategoryId,
    string PreviousSlug,
    string CurrentSlug) : DomainEvent;
