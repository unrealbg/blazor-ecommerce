namespace BuildingBlocks.Application.Contracts;

public sealed record InventoryReservationIssue(
    Guid ProductId,
    string Code,
    string Message);
