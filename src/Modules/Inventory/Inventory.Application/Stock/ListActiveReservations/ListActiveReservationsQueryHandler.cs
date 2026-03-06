using BuildingBlocks.Application.Abstractions;

namespace Inventory.Application.Stock.ListActiveReservations;

public sealed class ListActiveReservationsQueryHandler(IStockReservationRepository stockReservationRepository)
    : IQueryHandler<ListActiveReservationsQuery, InventoryPage<StockReservationDto>>
{
    public async Task<InventoryPage<StockReservationDto>> Handle(
        ListActiveReservationsQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedPage = request.Page <= 0 ? 1 : request.Page;
        var normalizedPageSize = request.PageSize <= 0 ? 50 : Math.Min(200, request.PageSize);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var (items, totalCount) = await stockReservationRepository.ListActivePageAsync(
            request.ProductId,
            skip,
            normalizedPageSize,
            cancellationToken);

        var mappedItems = items
            .Select(item => new StockReservationDto(
                item.Id,
                item.ProductId,
                item.Sku,
                item.CartId,
                item.CustomerId,
                item.OrderId,
                item.Quantity,
                item.Status.ToString(),
                item.ExpiresAtUtc,
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                item.ReservationToken))
            .ToList();

        return new InventoryPage<StockReservationDto>(
            normalizedPage,
            normalizedPageSize,
            totalCount,
            mappedItems);
    }
}
