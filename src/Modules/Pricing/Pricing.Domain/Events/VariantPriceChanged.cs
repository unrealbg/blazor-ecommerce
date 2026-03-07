using BuildingBlocks.Domain.Primitives;

namespace Pricing.Domain.Events;

public sealed record VariantPriceChanged(Guid VariantId, Guid VariantPriceId) : DomainEvent;
