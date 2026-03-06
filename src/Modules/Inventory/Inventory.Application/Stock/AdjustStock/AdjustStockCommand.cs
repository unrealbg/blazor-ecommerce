using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Inventory.Application.Stock.AdjustStock;

public sealed record AdjustStockCommand(
    Guid ProductId,
    int QuantityDelta,
    string? Reason,
    string? CreatedBy) : ICommand<bool>;
