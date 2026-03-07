namespace BuildingBlocks.Application.Contracts;

public sealed record OrderReviewVerificationResult(bool IsVerified, Guid? OrderId);
