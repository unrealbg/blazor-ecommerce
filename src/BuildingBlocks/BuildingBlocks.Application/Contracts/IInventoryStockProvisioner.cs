using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Contracts;

public interface IInventoryStockProvisioner
{
    Task<Result> EnsureStockItemAsync(
        Guid productId,
        Guid variantId,
        string? sku,
        int initialOnHandQuantity,
        bool isTracked,
        bool allowBackorder,
        CancellationToken cancellationToken);
}
