using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Pricing.Domain.Events;

namespace Pricing.Domain.Coupons;

public sealed class Coupon : AggregateRoot<Guid>
{
    private Coupon()
    {
    }

    private Coupon(
        Guid id,
        string code,
        string? description,
        Guid promotionId,
        CouponStatus status,
        DateTime? startAtUtc,
        DateTime? endAtUtc,
        int? usageLimitTotal,
        int? usageLimitPerCustomer,
        DateTime createdAtUtc)
    {
        Id = id;
        Code = code;
        Description = description;
        PromotionId = promotionId;
        Status = status;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        UsageLimitTotal = usageLimitTotal;
        UsageLimitPerCustomer = usageLimitPerCustomer;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public string Code { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid PromotionId { get; private set; }

    public CouponStatus Status { get; private set; }

    public DateTime? StartAtUtc { get; private set; }

    public DateTime? EndAtUtc { get; private set; }

    public int? UsageLimitTotal { get; private set; }

    public int? UsageLimitPerCustomer { get; private set; }

    public int TimesUsedTotal { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<Coupon> Create(
        string code,
        string? description,
        Guid promotionId,
        DateTime? startAtUtc,
        DateTime? endAtUtc,
        int? usageLimitTotal,
        int? usageLimitPerCustomer,
        DateTime createdAtUtc)
    {
        var validation = Validate(code, promotionId, startAtUtc, endAtUtc);
        if (validation.IsFailure)
        {
            return Result<Coupon>.Failure(validation.Error);
        }

        return Result<Coupon>.Success(new Coupon(
            Guid.NewGuid(),
            NormalizeCode(code),
            NormalizeText(description),
            promotionId,
            CouponStatus.Active,
            startAtUtc,
            endAtUtc,
            usageLimitTotal,
            usageLimitPerCustomer,
            createdAtUtc));
    }

    public Result Update(
        string? description,
        DateTime? startAtUtc,
        DateTime? endAtUtc,
        int? usageLimitTotal,
        int? usageLimitPerCustomer,
        DateTime updatedAtUtc)
    {
        var validation = Validate(Code, PromotionId, startAtUtc, endAtUtc);
        if (validation.IsFailure)
        {
            return validation;
        }

        Description = NormalizeText(description);
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        UsageLimitTotal = usageLimitTotal;
        UsageLimitPerCustomer = usageLimitPerCustomer;
        UpdatedAtUtc = updatedAtUtc;
        return Result.Success();
    }

    public void Disable(DateTime updatedAtUtc)
    {
        Status = CouponStatus.Disabled;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Archive(DateTime updatedAtUtc)
    {
        Status = CouponStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;
    }

    public bool IsActiveAt(DateTime utcNow)
    {
        if (Status != CouponStatus.Active)
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

    public void RegisterRedemption(Guid orderId, string? customerId, decimal discountAmount, DateTime createdAtUtc)
    {
        TimesUsedTotal++;
        UpdatedAtUtc = createdAtUtc;
        RaiseDomainEvent(new CouponRedeemed(Id, Code, PromotionId, orderId, customerId, discountAmount));
    }

    private static Result Validate(string code, Guid promotionId, DateTime? startAtUtc, DateTime? endAtUtc)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure(new Error("pricing.coupon.code.required", "Coupon code is required."));
        }

        if (promotionId == Guid.Empty)
        {
            return Result.Failure(new Error("pricing.coupon.promotion.required", "Promotion id is required."));
        }

        if (startAtUtc is not null && endAtUtc is not null && startAtUtc > endAtUtc)
        {
            return Result.Failure(new Error("pricing.coupon.window.invalid", "Coupon validity window is invalid."));
        }

        return Result.Success();
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
