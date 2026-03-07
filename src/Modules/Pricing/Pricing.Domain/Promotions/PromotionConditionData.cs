namespace Pricing.Domain.Promotions;

public sealed record PromotionConditionData(
    PromotionConditionType ConditionType,
    PromotionConditionOperator Operator,
    string Value);
