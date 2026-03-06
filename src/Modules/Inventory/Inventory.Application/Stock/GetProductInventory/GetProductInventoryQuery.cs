using BuildingBlocks.Application.Abstractions;

namespace Inventory.Application.Stock.GetProductInventory;

public sealed record GetProductInventoryQuery(Guid ProductId) : IQuery<InventoryProductDetailsDto?>;
