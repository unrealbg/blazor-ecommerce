using BuildingBlocks.Application.Abstractions;

namespace Inventory.Application.Stock.ListStockMovements;

public sealed class ListStockMovementsQueryHandler(IStockMovementRepository stockMovementRepository)
    : IQueryHandler<ListStockMovementsQuery, InventoryPage<StockMovementDto>>
{
    public async Task<InventoryPage<StockMovementDto>> Handle(
        ListStockMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedPage = request.Page <= 0 ? 1 : request.Page;
        var normalizedPageSize = request.PageSize <= 0 ? 50 : Math.Min(200, request.PageSize);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var (items, totalCount) = await stockMovementRepository.ListByProductPageAsync(
            request.ProductId,
            skip,
            normalizedPageSize,
            cancellationToken);

        var mappedItems = items
            .Select(item => new StockMovementDto(
                item.Id,
                item.ProductId,
                item.Sku,
                item.Type.ToString(),
                item.QuantityDelta,
                item.ReferenceId,
                item.Reason,
                item.CreatedAtUtc,
                item.CreatedBy))
            .ToList();

        return new InventoryPage<StockMovementDto>(
            normalizedPage,
            normalizedPageSize,
            totalCount,
            mappedItems);
    }
}
