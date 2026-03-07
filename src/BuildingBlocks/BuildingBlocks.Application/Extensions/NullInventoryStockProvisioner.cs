using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullInventoryStockProvisioner : IInventoryStockProvisioner
{
    public Task<Result> EnsureStockItemAsync(
        Guid productId,
        Guid variantId,
        string? sku,
        int initialOnHandQuantity,
        bool isTracked,
        bool allowBackorder,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }
}
