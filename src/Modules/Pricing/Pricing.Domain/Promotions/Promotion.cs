using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Pricing.Domain.Coupons;
using Pricing.Domain.Events;

namespace Pricing.Domain.Promotions;

public sealed class Promotion : AggregateRoot<Guid>
{
    private readonly List<PromotionScope> scopes = [];
    private readonly List<PromotionCondition> conditions = [];
    private readonly List<PromotionBenefit> benefits = [];

    private Promotion()
    {
    }

    private Promotion(
        Guid id,
        string name,
        string? code,
        PromotionType type,
        PromotionStatus status,
        string? description,
        int priority,
        bool isExclusive,
        bool allowWithCoupons,
        DateTime? startAtUtc,
        DateTime? endAtUtc,
        int? usageLimitTotal,
        int? usageLimitPerCustomer,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Code = code;
        Type = type;
        Status = status;
        Description = description;
        Priority = priority;
        IsExclusive = isExclusive;
        AllowWithCoupons = allowWithCoupons;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        UsageLimitTotal = usageLimitTotal;
        UsageLimitPerCustomer = usageLimitPerCustomer;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;

    public string? Code { get; private set; }

    public PromotionType Type { get; private set; }

    public PromotionStatus Status { get; private set; }

    public string? Description { get; private set; }

    public int Priority { get; private set; }

    public bool IsExclusive { get; private set; }

    public bool AllowWithCoupons { get; private set; }

    public DateTime? StartAtUtc { get; private set; }

    public DateTime? EndAtUtc { get; private set; }

    public int? UsageLimitTotal { get; private set; }

    public int? UsageLimitPerCustomer { get; private set; }

    public int TimesUsedTotal { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<PromotionScope> Scopes => scopes.AsReadOnly();

    public IReadOnlyCollection<PromotionCondition> Conditions => conditions.AsReadOnly();

    public IReadOnlyCollection<PromotionBenefit> Benefits => benefits.AsReadOnly();

    public static Result<Promotion> Create(
        string name,
        string? code,
        PromotionType type,
        string? description,
        int priority,
        bool isExclusive,
        bool allowWithCoupons,
        DateTime? startAtUtc,
        DateTime? endAtUtc,
        int? usageLimitTotal,
        int? usageLimitPerCustomer,
        IReadOnlyCollection<PromotionScopeData> scopeData,
        IReadOnlyCollection<PromotionConditionData> conditionData,
        IReadOnlyCollection<PromotionBenefitData> benefitData,
        DateTime createdAtUtc)
    {
        var validation = Validate(name, startAtUtc, endAtUtc, benefitData);
        if (validation.IsFailure)
        {
            return Result<Promotion>.Failure(validation.Error);
        }

        var promotion = new Promotion(
            Guid.NewGuid(),
            name.Trim(),
            NormalizeCode(code),
            type,
            PromotionStatus.Draft,
            NormalizeText(description),
            priority,
            isExclusive,
            allowWithCoupons,
            startAtUtc,
            endAtUtc,
            usageLimitTotal,
            usageLimitPerCustomer,
            createdAtUtc);

        promotion.ReplaceCollections(scopeData, conditionData, benefitData);
        return Result<Promotion>.Success(promotion);
    }

    public Result Update(
        string name,
        string? code,
        string? description,
        int priority,
        bool isExclusive,
        bool allowWithCoupons,
        DateTime? startAtUtc,
        DateTime? endAtUtc,
        int? usageLimitTotal,
        int? usageLimitPerCustomer,
        IReadOnlyCollection<PromotionScopeData> scopeData,
        IReadOnlyCollection<PromotionConditionData> conditionData,
        IReadOnlyCollection<PromotionBenefitData> benefitData,
        DateTime updatedAtUtc)
    {
        var validation = Validate(name, startAtUtc, endAtUtc, benefitData);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Code = NormalizeCode(code);
        Description = NormalizeText(description);
        Priority = priority;
        IsExclusive = isExclusive;
        AllowWithCoupons = allowWithCoupons;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        UsageLimitTotal = usageLimitTotal;
        UsageLimitPerCustomer = usageLimitPerCustomer;
        UpdatedAtUtc = updatedAtUtc;
        ReplaceCollections(scopeData, conditionData, benefitData);
        return Result.Success();
    }

    public Result Activate(DateTime updatedAtUtc)
    {
        if (Status == PromotionStatus.Archived)
        {
            return Result.Failure(new Error(
                "pricing.promotion.transition.invalid",
                "Archived promotion cannot be activated."));
        }

        Status = PromotionStatus.Active;
        UpdatedAtUtc = updatedAtUtc;
        RaiseDomainEvent(new PromotionActivated(Id));
        return Result.Success();
    }

    public Result Archive(DateTime updatedAtUtc)
    {
        Status = PromotionStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;
        RaiseDomainEvent(new PromotionArchived(Id));
        return Result.Success();
    }

    public bool IsActiveAt(DateTime utcNow)
    {
        if (Status != PromotionStatus.Active)
        {
            return false;
        }

        if (StartAtUtc is not null && utcNow < StartAtUtc.Value)
        {
            return false;
        }

        return EndAtUtc is null || utcNow <= EndAtUtc.Value;
    }

    public bool CanRedeemForCustomer(int customerRedemptions)
    {
        if (UsageLimitTotal is not null && TimesUsedTotal >= UsageLimitTotal.Value)
        {
            return false;
        }

        return UsageLimitPerCustomer is null || customerRedemptions < UsageLimitPerCustomer.Value;
    }

    public void RegisterRedemption(Coupon? coupon, Guid orderId, string? customerId, decimal discountAmount, DateTime createdAtUtc)
    {
        TimesUsedTotal++;
        UpdatedAtUtc = createdAtUtc;
        RaiseDomainEvent(new PromotionRedeemed(Id, coupon?.Id, orderId, customerId, discountAmount));
    }

    private void ReplaceCollections(
        IReadOnlyCollection<PromotionScopeData> scopeData,
        IReadOnlyCollection<PromotionConditionData> conditionData,
        IReadOnlyCollection<PromotionBenefitData> benefitData)
    {
        scopes.Clear();
        conditions.Clear();
        benefits.Clear();

        foreach (var scope in scopeData)
        {
            scopes.Add(PromotionScope.Create(Id, scope.ScopeType, scope.TargetId));
        }

        foreach (var condition in conditionData)
        {
            conditions.Add(PromotionCondition.Create(Id, condition.ConditionType, condition.Operator, condition.Value));
        }

        foreach (var benefit in benefitData)
        {
            benefits.Add(PromotionBenefit.Create(
                Id,
                benefit.BenefitType,
                benefit.ValueAmount,
                benefit.ValuePercent,
                benefit.MaxDiscountAmount,
                benefit.ApplyPerUnit));
        }
    }

    private static Result Validate(
        string name,
        DateTime? startAtUtc,
        DateTime? endAtUtc,
        IReadOnlyCollection<PromotionBenefitData> benefitData)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(new Error("pricing.promotion.name.required", "Promotion name is required."));
        }

        if (startAtUtc is not null && endAtUtc is not null && startAtUtc > endAtUtc)
        {
            return Result.Failure(new Error("pricing.promotion.window.invalid", "Promotion validity window is invalid."));
        }

        if (benefitData.Count == 0)
        {
            return Result.Failure(new Error("pricing.promotion.benefit.required", "At least one promotion benefit is required."));
        }

        return Result.Success();
    }

    private static string? NormalizeCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
