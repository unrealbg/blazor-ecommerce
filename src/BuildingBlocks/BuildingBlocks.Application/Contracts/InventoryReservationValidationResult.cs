namespace BuildingBlocks.Application.Contracts;

public sealed record InventoryReservationValidationResult(
    bool IsValid,
    IReadOnlyCollection<InventoryReservationIssue> Issues);
