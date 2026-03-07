namespace BuildingBlocks.Application.Contracts;

public sealed record CouponValidationResult(
    string Code,
    bool IsValid,
    string? ErrorCode,
    string? ErrorMessage,
    string? PromotionName);
