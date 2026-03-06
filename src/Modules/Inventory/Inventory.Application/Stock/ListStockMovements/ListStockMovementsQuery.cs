using BuildingBlocks.Application.Abstractions;

namespace Inventory.Application.Stock.ListStockMovements;

public sealed record ListStockMovementsQuery(
    Guid ProductId,
    int Page = 1,
    int PageSize = 50) : IQuery<InventoryPage<StockMovementDto>>;
