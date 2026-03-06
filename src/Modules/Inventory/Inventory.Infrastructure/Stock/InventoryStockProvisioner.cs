using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Inventory.Application.Stock;
using Inventory.Domain.Stock;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure.Stock;

internal sealed class InventoryStockProvisioner(
    IStockItemRepository stockItemRepository,
    IInventoryUnitOfWork unitOfWork,
    IClock clock,
    IOptions<InventoryModuleOptions> options)
    : IInventoryStockProvisioner
{
    private readonly InventoryModuleOptions options = options.Value;

    public async Task<Result> EnsureStockItemAsync(
        Guid productId,
        string? sku,
        int initialOnHandQuantity,
        bool isTracked,
        bool allowBackorder,
        CancellationToken cancellationToken)
    {
        var result = await unitOfWork.ExecuteWithConcurrencyRetryAsync(
            async innerCancellationToken =>
            {
                var existing = await stockItemRepository.GetByProductAndSkuAsync(
                    productId,
                    sku,
                    innerCancellationToken);

                if (existing is null)
                {
                    var createResult = StockItem.Create(
                        productId,
                        sku,
                        Math.Max(0, initialOnHandQuantity),
                        isTracked,
                        allowBackorder,
                        clock.UtcNow);
                    if (createResult.IsFailure)
                    {
                        return Result<bool>.Failure(createResult.Error);
                    }

                    await stockItemRepository.AddAsync(createResult.Value, innerCancellationToken);
                    await unitOfWork.SaveChangesAsync(innerCancellationToken);
                    return Result<bool>.Success(true);
                }

                var updateResult = existing.UpdateTracking(isTracked, allowBackorder, clock.UtcNow);
                if (updateResult.IsFailure)
                {
                    return Result<bool>.Failure(updateResult.Error);
                }

                await unitOfWork.SaveChangesAsync(innerCancellationToken);
                return Result<bool>.Success(true);
            },
            Math.Max(1, this.options.RetryOnConcurrencyCount),
            cancellationToken);

        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.Error);
    }
}
