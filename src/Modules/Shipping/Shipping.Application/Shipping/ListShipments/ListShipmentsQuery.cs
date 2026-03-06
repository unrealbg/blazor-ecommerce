using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.ListShipments;

public sealed record ListShipmentsQuery(
    string? Status,
    Guid? OrderId,
    int Page,
    int PageSize) : IQuery<ShippingPage<ShipmentDto>>;
