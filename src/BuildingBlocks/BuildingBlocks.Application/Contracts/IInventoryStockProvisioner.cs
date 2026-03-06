using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Contracts;

public interface IInventoryStockProvisioner
{
    Task<Result> EnsureStockItemAsync(
        Guid productId,
        string? sku,
        int initialOnHandQuantity,
        bool isTracked,
        bool allowBackorder,
        CancellationToken cancellationToken);
}
