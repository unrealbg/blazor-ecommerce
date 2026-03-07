namespace BuildingBlocks.Application.Contracts;

public sealed record CartPricingLineRequest(
    Guid ProductId,
    Guid VariantId,
    int Quantity);
