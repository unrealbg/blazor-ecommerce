using BuildingBlocks.Application.Abstractions;

namespace Inventory.Application.Stock.ListActiveReservations;

public sealed record ListActiveReservationsQuery(
    Guid? ProductId,
    int Page = 1,
    int PageSize = 50) : IQuery<InventoryPage<StockReservationDto>>;
