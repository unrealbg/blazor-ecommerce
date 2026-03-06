namespace Inventory.Application.Stock;

public sealed record InventoryPage<TItem>(
    int Page,
    int PageSize,
    long TotalCount,
    IReadOnlyCollection<TItem> Items);
