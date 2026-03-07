using BuildingBlocks.Domain.Primitives;

namespace Pricing.Domain.Promotions;

public sealed class PromotionCondition : Entity<Guid>
{
    private PromotionCondition()
    {
    }

    private PromotionCondition(
        Guid id,
        Guid promotionId,
        PromotionConditionType conditionType,
        PromotionConditionOperator @operator,
        string value)
    {
        Id = id;
        PromotionId = promotionId;
        ConditionType = conditionType;
        Operator = @operator;
        Value = value;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid PromotionId { get; private set; }

    public PromotionConditionType ConditionType { get; private set; }

    public PromotionConditionOperator Operator { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    internal static PromotionCondition Create(
        Guid promotionId,
        PromotionConditionType conditionType,
        PromotionConditionOperator @operator,
        string value)
    {
        return new PromotionCondition(Guid.NewGuid(), promotionId, conditionType, @operator, value.Trim());
    }
}
