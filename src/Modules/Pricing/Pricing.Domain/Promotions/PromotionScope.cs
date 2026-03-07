using BuildingBlocks.Domain.Primitives;

namespace Pricing.Domain.Promotions;

public sealed class PromotionScope : Entity<Guid>
{
    private PromotionScope()
    {
    }

    private PromotionScope(Guid id, Guid promotionId, PromotionScopeType scopeType, Guid? targetId)
    {
        Id = id;
        PromotionId = promotionId;
        ScopeType = scopeType;
        TargetId = targetId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid PromotionId { get; private set; }

    public PromotionScopeType ScopeType { get; private set; }

    public Guid? TargetId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    internal static PromotionScope Create(Guid promotionId, PromotionScopeType scopeType, Guid? targetId)
    {
        return new PromotionScope(Guid.NewGuid(), promotionId, scopeType, targetId);
    }
}
