using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Inventory.Domain.Stock;
using Microsoft.Extensions.Options;

namespace Inventory.Application.Stock.AdjustStock;

public sealed class AdjustStockCommandHandler(
    IStockItemRepository stockItemRepository,
    IStockMovementRepository stockMovementRepository,
    IInventoryUnitOfWork unitOfWork,
    IClock clock,
    IOptions<InventoryModuleOptions> options)
    : ICommandHandler<AdjustStockCommand, bool>
{
    private readonly InventoryModuleOptions options = options.Value;

    public Task<Result<bool>> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteWithConcurrencyRetryAsync(
            async innerCancellationToken =>
            {
                var stockItem = await stockItemRepository.GetByProductAndSkuAsync(
                    request.ProductId,
                    sku: null,
                    innerCancellationToken);
                if (stockItem is null)
                {
                    return Result<bool>.Failure(new Error(
                        "inventory.stock_item.not_found",
                        "Stock item was not found."));
                }

                var adjustResult = stockItem.AdjustOnHand(request.QuantityDelta, clock.UtcNow);
                if (adjustResult.IsFailure)
                {
                    return Result<bool>.Failure(adjustResult.Error);
                }

                var movementResult = StockMovement.Create(
                    stockItem.ProductId,
                    stockItem.Sku,
                    request.QuantityDelta > 0 ? StockMovementType.Restock : StockMovementType.ManualAdjustment,
                    request.QuantityDelta,
                    referenceId: null,
                    request.Reason,
                    request.CreatedBy,
                    clock.UtcNow);
                if (movementResult.IsFailure)
                {
                    return Result<bool>.Failure(movementResult.Error);
                }

                await stockMovementRepository.AddAsync(movementResult.Value, innerCancellationToken);
                await unitOfWork.SaveChangesAsync(innerCancellationToken);
                return Result<bool>.Success(true);
            },
            Math.Max(1, this.options.RetryOnConcurrencyCount),
            cancellationToken);
    }
}
