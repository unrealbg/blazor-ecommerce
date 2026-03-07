namespace BuildingBlocks.Application.Contracts;

public sealed record ProductOptionSelectionSnapshot(
    Guid ProductOptionId,
    string OptionName,
    Guid ProductOptionValueId,
    string Value);
