using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Inventory.Application.Stock;
using Inventory.Domain.Stock;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure.Stock;

internal sealed class InventoryReservationService(
    IStockItemRepository stockItemRepository,
    IStockReservationRepository stockReservationRepository,
    IStockMovementRepository stockMovementRepository,
    IInventoryUnitOfWork unitOfWork,
    IClock clock,
    IOptions<InventoryModuleOptions> options)
    : IInventoryReservationService
{
    private readonly InventoryModuleOptions options = options.Value;

    public Task<Result> SyncCartReservationAsync(
        string cartId,
        Guid productId,
        string? sku,
        int quantity,
        CancellationToken cancellationToken)
    {
        var normalizedCartId = NormalizeCartId(cartId);
        if (normalizedCartId.IsFailure)
        {
            return Task.FromResult(Result.Failure(normalizedCartId.Error));
        }

        if (quantity < 0)
        {
            return Task.FromResult(Result.Failure(new Error(
                "inventory.reservation.quantity.invalid",
                "Quantity must be zero or positive.")));
        }

        return SyncCartReservationInternalAsync(
            normalizedCartId.Value,
            productId,
            sku,
            quantity,
            cancellationToken);
    }

    public Task<Result> ReleaseAllCartReservationsAsync(string cartId, CancellationToken cancellationToken)
    {
        var normalizedCartId = NormalizeCartId(cartId);
        if (normalizedCartId.IsFailure)
        {
            return Task.FromResult(Result.Failure(normalizedCartId.Error));
        }

        return ReleaseAllInternalAsync(normalizedCartId.Value, cancellationToken);
    }

    public async Task<Result<InventoryReservationValidationResult>> ValidateCartReservationsAsync(
        string cartId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken)
    {
        var normalizedCartId = NormalizeCartId(cartId);
        if (normalizedCartId.IsFailure)
        {
            return Result<InventoryReservationValidationResult>.Failure(normalizedCartId.Error);
        }

        if (lines.Count == 0)
        {
            return Result<InventoryReservationValidationResult>.Success(
                new InventoryReservationValidationResult(true, []));
        }

        var issues = new List<InventoryReservationIssue>();
        var utcNow = clock.UtcNow;
        foreach (var line in NormalizeLines(lines))
        {
            var stockItem = await stockItemRepository.GetByProductAndSkuAsync(
                line.ProductId,
                line.Sku,
                cancellationToken);
            if (stockItem is null)
            {
                issues.Add(new InventoryReservationIssue(
                    line.ProductId,
                    "inventory.stock_item.not_found",
                    "Inventory item was not found."));
                continue;
            }

            if (!stockItem.IsTracked)
            {
                continue;
            }

            var reservation = await stockReservationRepository.GetActiveByCartProductSkuAsync(
                normalizedCartId.Value,
                line.ProductId,
                line.Sku,
                cancellationToken);

            if (reservation is null)
            {
                issues.Add(new InventoryReservationIssue(
                    line.ProductId,
                    "inventory.reservation.not_found",
                    "Stock reservation was not found."));
                continue;
            }

            if (reservation.ExpiresAtUtc <= utcNow)
            {
                issues.Add(new InventoryReservationIssue(
                    line.ProductId,
                    "inventory.reservation.expired",
                    "Stock reservation has expired."));
                continue;
            }

            if (reservation.Quantity < line.Quantity)
            {
                issues.Add(new InventoryReservationIssue(
                    line.ProductId,
                    "inventory.stock.insufficient",
                    "Reserved quantity is lower than cart quantity."));
            }
        }

        var result = new InventoryReservationValidationResult(issues.Count == 0, issues);
        return Result<InventoryReservationValidationResult>.Success(result);
    }

    public Task<Result> ConsumeCartReservationsAsync(
        string cartId,
        Guid orderId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken)
    {
        var normalizedCartId = NormalizeCartId(cartId);
        if (normalizedCartId.IsFailure)
        {
            return Task.FromResult(Result.Failure(normalizedCartId.Error));
        }

        if (orderId == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(new Error(
                "inventory.order_id.required",
                "Order id is required.")));
        }

        return ConsumeInternalAsync(normalizedCartId.Value, orderId, lines, cancellationToken);
    }

    public Task<Result> PromoteCartReservationsToOrderAsync(
        string cartId,
        Guid orderId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken)
    {
        var normalizedCartId = NormalizeCartId(cartId);
        if (normalizedCartId.IsFailure)
        {
            return Task.FromResult(Result.Failure(normalizedCartId.Error));
        }

        if (orderId == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(new Error(
                "inventory.order_id.required",
                "Order id is required.")));
        }

        return PromoteInternalAsync(normalizedCartId.Value, orderId, lines, cancellationToken);
    }

    public Task<Result> ConsumeOrderReservationsAsync(
        Guid orderId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken)
    {
        if (orderId == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(new Error(
                "inventory.order_id.required",
                "Order id is required.")));
        }

        return ConsumeOrderInternalAsync(orderId, lines, cancellationToken);
    }

    public Task<Result> ReleaseOrderReservationsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        if (orderId == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(new Error(
                "inventory.order_id.required",
                "Order id is required.")));
        }

        return ReleaseOrderInternalAsync(orderId, cancellationToken);
    }

    private async Task<Result> SyncCartReservationInternalAsync(
        string cartId,
        Guid productId,
        string? sku,
        int quantity,
        CancellationToken cancellationToken)
    {
        var operationResult = await unitOfWork.ExecuteWithConcurrencyRetryAsync(
            async innerCancellationToken =>
            {
                var stockItem = await stockItemRepository.GetByProductAndSkuAsync(
                    productId,
                    sku,
                    innerCancellationToken);
                if (stockItem is null)
                {
                    return Result<bool>.Failure(new Error(
                        "inventory.stock_item.not_found",
                        "Inventory item was not found."));
                }

                var existingReservation = await stockReservationRepository.GetActiveByCartProductSkuAsync(
                    cartId,
                    productId,
                    sku,
                    innerCancellationToken);
                var currentQuantity = existingReservation?.Quantity ?? 0;

                if (!stockItem.IsTracked)
                {
                    if (existingReservation is not null)
                    {
                        var releaseResult = existingReservation.Release(clock.UtcNow);
                        if (releaseResult.IsFailure)
                        {
                            return Result<bool>.Failure(releaseResult.Error);
                        }

                        var movementResult = StockMovement.Create(
                            stockItem.ProductId,
                            stockItem.Sku,
                            StockMovementType.ReservationReleased,
                            Math.Abs(currentQuantity),
                            existingReservation.Id,
                            "Inventory tracking disabled",
                            "system",
                            clock.UtcNow);
                        if (movementResult.IsFailure)
                        {
                            return Result<bool>.Failure(movementResult.Error);
                        }

                        await stockMovementRepository.AddAsync(movementResult.Value, innerCancellationToken);
                    }

                    await unitOfWork.SaveChangesAsync(innerCancellationToken);
                    return Result<bool>.Success(true);
                }

                if (quantity == 0)
                {
                    if (existingReservation is null)
                    {
                        return Result<bool>.Success(true);
                    }

                    var stockReleaseResult = stockItem.Release(currentQuantity, clock.UtcNow);
                    if (stockReleaseResult.IsFailure)
                    {
                        return Result<bool>.Failure(stockReleaseResult.Error);
                    }

                    var reservationReleaseResult = existingReservation.Release(clock.UtcNow);
                    if (reservationReleaseResult.IsFailure)
                    {
                        return Result<bool>.Failure(reservationReleaseResult.Error);
                    }

                    var movementResult = StockMovement.Create(
                        stockItem.ProductId,
                        stockItem.Sku,
                        StockMovementType.ReservationReleased,
                        Math.Abs(currentQuantity),
                        existingReservation.Id,
                        "Cart line removed",
                        "system",
                        clock.UtcNow);
                    if (movementResult.IsFailure)
                    {
                        return Result<bool>.Failure(movementResult.Error);
                    }

                    await stockMovementRepository.AddAsync(movementResult.Value, innerCancellationToken);
                    await unitOfWork.SaveChangesAsync(innerCancellationToken);
                    return Result<bool>.Success(true);
                }

                var quantityDelta = quantity - currentQuantity;
                if (quantityDelta > 0)
                {
                    var reserveResult = stockItem.Reserve(quantityDelta, clock.UtcNow);
                    if (reserveResult.IsFailure)
                    {
                        return Result<bool>.Failure(reserveResult.Error);
                    }

                    if (existingReservation is null)
                    {
                        var createReservationResult = StockReservation.Create(
                            productId,
                            sku,
                            cartId,
                            customerId: null,
                            quantity,
                            Guid.NewGuid().ToString("N"),
                            clock.UtcNow.Add(this.options.ReservationTtl),
                            clock.UtcNow);
                        if (createReservationResult.IsFailure)
                        {
                            return Result<bool>.Failure(createReservationResult.Error);
                        }

                        await stockReservationRepository.AddAsync(createReservationResult.Value, innerCancellationToken);
                        existingReservation = createReservationResult.Value;
                    }
                    else
                    {
                        var updateResult = existingReservation.SetQuantity(quantity, clock.UtcNow);
                        if (updateResult.IsFailure)
                        {
                            return Result<bool>.Failure(updateResult.Error);
                        }
                    }

                    if (this.options.RefreshReservationOnCartMutation)
                    {
                        var refreshResult = existingReservation.RefreshExpiration(
                            clock.UtcNow.Add(this.options.ReservationTtl),
                            clock.UtcNow);
                        if (refreshResult.IsFailure)
                        {
                            return Result<bool>.Failure(refreshResult.Error);
                        }
                    }

                    var movementResult = StockMovement.Create(
                        stockItem.ProductId,
                        stockItem.Sku,
                        StockMovementType.ReservationCreated,
                        -quantityDelta,
                        existingReservation.Id,
                        "Cart quantity increased",
                        "system",
                        clock.UtcNow);
                    if (movementResult.IsFailure)
                    {
                        return Result<bool>.Failure(movementResult.Error);
                    }

                    await stockMovementRepository.AddAsync(movementResult.Value, innerCancellationToken);
                    await unitOfWork.SaveChangesAsync(innerCancellationToken);
                    return Result<bool>.Success(true);
                }

                if (quantityDelta < 0)
                {
                    if (existingReservation is null)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.reservation.not_found",
                            "Stock reservation was not found."));
                    }

                    var releaseQuantity = Math.Abs(quantityDelta);
                    var stockReleaseResult = stockItem.Release(releaseQuantity, clock.UtcNow);
                    if (stockReleaseResult.IsFailure)
                    {
                        return Result<bool>.Failure(stockReleaseResult.Error);
                    }

                    var updateResult = existingReservation.SetQuantity(quantity, clock.UtcNow);
                    if (updateResult.IsFailure)
                    {
                        return Result<bool>.Failure(updateResult.Error);
                    }

                    if (this.options.RefreshReservationOnCartMutation)
                    {
                        var refreshResult = existingReservation.RefreshExpiration(
                            clock.UtcNow.Add(this.options.ReservationTtl),
                            clock.UtcNow);
                        if (refreshResult.IsFailure)
                        {
                            return Result<bool>.Failure(refreshResult.Error);
                        }
                    }

                    var movementResult = StockMovement.Create(
                        stockItem.ProductId,
                        stockItem.Sku,
                        StockMovementType.ReservationReleased,
                        releaseQuantity,
                        existingReservation.Id,
                        "Cart quantity decreased",
                        "system",
                        clock.UtcNow);
                    if (movementResult.IsFailure)
                    {
                        return Result<bool>.Failure(movementResult.Error);
                    }

                    await stockMovementRepository.AddAsync(movementResult.Value, innerCancellationToken);
                    await unitOfWork.SaveChangesAsync(innerCancellationToken);
                    return Result<bool>.Success(true);
                }

                if (existingReservation is not null && this.options.RefreshReservationOnCartMutation)
                {
                    var refreshResult = existingReservation.RefreshExpiration(
                        clock.UtcNow.Add(this.options.ReservationTtl),
                        clock.UtcNow);
                    if (refreshResult.IsFailure)
                    {
                        return Result<bool>.Failure(refreshResult.Error);
                    }

                    await unitOfWork.SaveChangesAsync(innerCancellationToken);
                }

                return Result<bool>.Success(true);
            },
            Math.Max(1, this.options.RetryOnConcurrencyCount),
            cancellationToken);

        return operationResult.IsSuccess
            ? Result.Success()
            : Result.Failure(operationResult.Error);
    }

    private async Task<Result> PromoteInternalAsync(
        string cartId,
        Guid orderId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken)
    {
        var normalizedLines = NormalizeLines(lines);
        if (normalizedLines.Count == 0)
        {
            return Result.Success();
        }

        var operationResult = await unitOfWork.ExecuteWithConcurrencyRetryAsync(
            async innerCancellationToken =>
            {
                foreach (var line in normalizedLines)
                {
                    var stockItem = await stockItemRepository.GetByProductAndSkuAsync(
                        line.ProductId,
                        line.Sku,
                        innerCancellationToken);
                    if (stockItem is null)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.stock_item.not_found",
                            "Inventory item was not found."));
                    }

                    if (!stockItem.IsTracked)
                    {
                        continue;
                    }

                    var reservation = await stockReservationRepository.GetActiveByCartProductSkuAsync(
                        cartId,
                        line.ProductId,
                        line.Sku,
                        innerCancellationToken);
                    if (reservation is null)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.reservation.not_found",
                            "Stock reservation was not found."));
                    }

                    if (reservation.ExpiresAtUtc <= clock.UtcNow)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.reservation.expired",
                            "Stock reservation has expired."));
                    }

                    if (reservation.Quantity < line.Quantity)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.stock.insufficient",
                            "Insufficient stock."));
                    }

                    if (reservation.Quantity > line.Quantity)
                    {
                        var releaseExtraQuantity = reservation.Quantity - line.Quantity;
                        var releaseStockResult = stockItem.Release(releaseExtraQuantity, clock.UtcNow);
                        if (releaseStockResult.IsFailure)
                        {
                            return Result<bool>.Failure(releaseStockResult.Error);
                        }

                        var reduceReservationResult = reservation.SetQuantity(line.Quantity, clock.UtcNow);
                        if (reduceReservationResult.IsFailure)
                        {
                            return Result<bool>.Failure(reduceReservationResult.Error);
                        }

                        var releaseMovementResult = StockMovement.Create(
                            reservation.ProductId,
                            reservation.Sku,
                            StockMovementType.ReservationReleased,
                            releaseExtraQuantity,
                            reservation.Id,
                            "Reduced reservation during checkout promotion",
                            "system",
                            clock.UtcNow);
                        if (releaseMovementResult.IsFailure)
                        {
                            return Result<bool>.Failure(releaseMovementResult.Error);
                        }

                        await stockMovementRepository.AddAsync(releaseMovementResult.Value, innerCancellationToken);
                    }

                    var assignResult = reservation.AssignToOrder(orderId, clock.UtcNow);
                    if (assignResult.IsFailure)
                    {
                        return Result<bool>.Failure(assignResult.Error);
                    }
                }

                await unitOfWork.SaveChangesAsync(innerCancellationToken);
                return Result<bool>.Success(true);
            },
            Math.Max(1, this.options.RetryOnConcurrencyCount),
            cancellationToken);

        return operationResult.IsSuccess
            ? Result.Success()
            : Result.Failure(operationResult.Error);
    }

    private async Task<Result> ReleaseAllInternalAsync(string cartId, CancellationToken cancellationToken)
    {
        var operationResult = await unitOfWork.ExecuteWithConcurrencyRetryAsync(
            async innerCancellationToken =>
            {
                var reservations = await stockReservationRepository.ListActiveByCartIdAsync(cartId, innerCancellationToken);
                if (reservations.Count == 0)
                {
                    return Result<bool>.Success(true);
                }

                foreach (var reservation in reservations)
                {
                    var stockItem = await stockItemRepository.GetByProductAndSkuAsync(
                        reservation.ProductId,
                        reservation.Sku,
                        innerCancellationToken);
                    if (stockItem is not null && stockItem.IsTracked)
                    {
                        var releaseStockResult = stockItem.Release(reservation.Quantity, clock.UtcNow);
                        if (releaseStockResult.IsFailure)
                        {
                            return Result<bool>.Failure(releaseStockResult.Error);
                        }
                    }

                    var releaseReservationResult = reservation.Release(clock.UtcNow);
                    if (releaseReservationResult.IsFailure)
                    {
                        return Result<bool>.Failure(releaseReservationResult.Error);
                    }

                    var movementResult = StockMovement.Create(
                        reservation.ProductId,
                        reservation.Sku,
                        StockMovementType.ReservationReleased,
                        reservation.Quantity,
                        reservation.Id,
                        "Cart cleared",
                        "system",
                        clock.UtcNow);
                    if (movementResult.IsFailure)
                    {
                        return Result<bool>.Failure(movementResult.Error);
                    }

                    await stockMovementRepository.AddAsync(movementResult.Value, innerCancellationToken);
                }

                await unitOfWork.SaveChangesAsync(innerCancellationToken);
                return Result<bool>.Success(true);
            },
            Math.Max(1, this.options.RetryOnConcurrencyCount),
            cancellationToken);

        return operationResult.IsSuccess
            ? Result.Success()
            : Result.Failure(operationResult.Error);
    }

    private async Task<Result> ConsumeInternalAsync(
        string cartId,
        Guid orderId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken)
    {
        var normalizedLines = NormalizeLines(lines);
        var operationResult = await unitOfWork.ExecuteWithConcurrencyRetryAsync(
            async innerCancellationToken =>
            {
                foreach (var line in normalizedLines)
                {
                    var stockItem = await stockItemRepository.GetByProductAndSkuAsync(
                        line.ProductId,
                        line.Sku,
                        innerCancellationToken);
                    if (stockItem is null)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.stock_item.not_found",
                            "Inventory item was not found."));
                    }

                    if (!stockItem.IsTracked)
                    {
                        continue;
                    }

                    var reservation = await stockReservationRepository.GetActiveByCartProductSkuAsync(
                        cartId,
                        line.ProductId,
                        line.Sku,
                        innerCancellationToken);
                    if (reservation is null)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.reservation.not_found",
                            "Stock reservation was not found."));
                    }

                    if (reservation.ExpiresAtUtc <= clock.UtcNow)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.reservation.expired",
                            "Stock reservation has expired."));
                    }

                    if (reservation.Quantity < line.Quantity)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.stock.insufficient",
                            "Insufficient stock."));
                    }

                    if (reservation.Quantity > line.Quantity)
                    {
                        var releaseExtraQuantity = reservation.Quantity - line.Quantity;

                        var releaseStockResult = stockItem.Release(releaseExtraQuantity, clock.UtcNow);
                        if (releaseStockResult.IsFailure)
                        {
                            return Result<bool>.Failure(releaseStockResult.Error);
                        }

                        var reduceReservationResult = reservation.SetQuantity(line.Quantity, clock.UtcNow);
                        if (reduceReservationResult.IsFailure)
                        {
                            return Result<bool>.Failure(reduceReservationResult.Error);
                        }

                        var releaseMovementResult = StockMovement.Create(
                            reservation.ProductId,
                            reservation.Sku,
                            StockMovementType.ReservationReleased,
                            releaseExtraQuantity,
                            reservation.Id,
                            "Checkout consumed lower quantity than reserved",
                            "system",
                            clock.UtcNow);
                        if (releaseMovementResult.IsFailure)
                        {
                            return Result<bool>.Failure(releaseMovementResult.Error);
                        }

                        await stockMovementRepository.AddAsync(releaseMovementResult.Value, innerCancellationToken);
                    }

                    var consumeStockResult = stockItem.Consume(line.Quantity, clock.UtcNow);
                    if (consumeStockResult.IsFailure)
                    {
                        return Result<bool>.Failure(consumeStockResult.Error);
                    }

                    var consumeReservationResult = reservation.Consume(orderId, clock.UtcNow);
                    if (consumeReservationResult.IsFailure)
                    {
                        return Result<bool>.Failure(consumeReservationResult.Error);
                    }

                    var reservationMovementResult = StockMovement.Create(
                        reservation.ProductId,
                        reservation.Sku,
                        StockMovementType.ReservationConsumed,
                        -line.Quantity,
                        reservation.Id,
                        "Checkout completed",
                        "system",
                        clock.UtcNow);
                    if (reservationMovementResult.IsFailure)
                    {
                        return Result<bool>.Failure(reservationMovementResult.Error);
                    }

                    await stockMovementRepository.AddAsync(reservationMovementResult.Value, innerCancellationToken);

                    var orderMovementResult = StockMovement.Create(
                        reservation.ProductId,
                        reservation.Sku,
                        StockMovementType.OrderCompleted,
                        -line.Quantity,
                        orderId,
                        "Order placed",
                        "system",
                        clock.UtcNow);
                    if (orderMovementResult.IsFailure)
                    {
                        return Result<bool>.Failure(orderMovementResult.Error);
                    }

                    await stockMovementRepository.AddAsync(orderMovementResult.Value, innerCancellationToken);
                }

                await unitOfWork.SaveChangesAsync(innerCancellationToken);
                return Result<bool>.Success(true);
            },
            Math.Max(1, this.options.RetryOnConcurrencyCount),
            cancellationToken);

        return operationResult.IsSuccess
            ? Result.Success()
            : Result.Failure(operationResult.Error);
    }

    private async Task<Result> ConsumeOrderInternalAsync(
        Guid orderId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken)
    {
        var normalizedLines = NormalizeLines(lines);
        if (normalizedLines.Count == 0)
        {
            return Result.Success();
        }

        var operationResult = await unitOfWork.ExecuteWithConcurrencyRetryAsync(
            async innerCancellationToken =>
            {
                foreach (var line in normalizedLines)
                {
                    var stockItem = await stockItemRepository.GetByProductAndSkuAsync(
                        line.ProductId,
                        line.Sku,
                        innerCancellationToken);
                    if (stockItem is null)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.stock_item.not_found",
                            "Inventory item was not found."));
                    }

                    if (!stockItem.IsTracked)
                    {
                        continue;
                    }

                    var reservation = await stockReservationRepository.GetActiveByOrderProductSkuAsync(
                        orderId,
                        line.ProductId,
                        line.Sku,
                        innerCancellationToken);
                    if (reservation is null)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.reservation.not_found",
                            "Stock reservation was not found."));
                    }

                    if (reservation.ExpiresAtUtc <= clock.UtcNow)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.reservation.expired",
                            "Stock reservation has expired."));
                    }

                    if (reservation.Quantity < line.Quantity)
                    {
                        return Result<bool>.Failure(new Error(
                            "inventory.stock.insufficient",
                            "Insufficient stock."));
                    }

                    if (reservation.Quantity > line.Quantity)
                    {
                        var releaseExtraQuantity = reservation.Quantity - line.Quantity;

                        var releaseStockResult = stockItem.Release(releaseExtraQuantity, clock.UtcNow);
                        if (releaseStockResult.IsFailure)
                        {
                            return Result<bool>.Failure(releaseStockResult.Error);
                        }

                        var reduceReservationResult = reservation.SetQuantity(line.Quantity, clock.UtcNow);
                        if (reduceReservationResult.IsFailure)
                        {
                            return Result<bool>.Failure(reduceReservationResult.Error);
                        }

                        var releaseMovementResult = StockMovement.Create(
                            reservation.ProductId,
                            reservation.Sku,
                            StockMovementType.ReservationReleased,
                            releaseExtraQuantity,
                            reservation.Id,
                            "Payment consumed lower quantity than reserved",
                            "system",
                            clock.UtcNow);
                        if (releaseMovementResult.IsFailure)
                        {
                            return Result<bool>.Failure(releaseMovementResult.Error);
                        }

                        await stockMovementRepository.AddAsync(releaseMovementResult.Value, innerCancellationToken);
                    }

                    var consumeStockResult = stockItem.Consume(line.Quantity, clock.UtcNow);
                    if (consumeStockResult.IsFailure)
                    {
                        return Result<bool>.Failure(consumeStockResult.Error);
                    }

                    var consumeReservationResult = reservation.Consume(orderId, clock.UtcNow);
                    if (consumeReservationResult.IsFailure)
                    {
                        return Result<bool>.Failure(consumeReservationResult.Error);
                    }

                    var reservationMovementResult = StockMovement.Create(
                        reservation.ProductId,
                        reservation.Sku,
                        StockMovementType.ReservationConsumed,
                        -line.Quantity,
                        reservation.Id,
                        "Payment captured",
                        "system",
                        clock.UtcNow);
                    if (reservationMovementResult.IsFailure)
                    {
                        return Result<bool>.Failure(reservationMovementResult.Error);
                    }

                    await stockMovementRepository.AddAsync(reservationMovementResult.Value, innerCancellationToken);

                    var orderMovementResult = StockMovement.Create(
                        reservation.ProductId,
                        reservation.Sku,
                        StockMovementType.OrderCompleted,
                        -line.Quantity,
                        orderId,
                        "Order paid",
                        "system",
                        clock.UtcNow);
                    if (orderMovementResult.IsFailure)
                    {
                        return Result<bool>.Failure(orderMovementResult.Error);
                    }

                    await stockMovementRepository.AddAsync(orderMovementResult.Value, innerCancellationToken);
                }

                await unitOfWork.SaveChangesAsync(innerCancellationToken);
                return Result<bool>.Success(true);
            },
            Math.Max(1, this.options.RetryOnConcurrencyCount),
            cancellationToken);

        return operationResult.IsSuccess
            ? Result.Success()
            : Result.Failure(operationResult.Error);
    }

    private async Task<Result> ReleaseOrderInternalAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var operationResult = await unitOfWork.ExecuteWithConcurrencyRetryAsync(
            async innerCancellationToken =>
            {
                var reservations = await stockReservationRepository.ListActiveByOrderIdAsync(
                    orderId,
                    innerCancellationToken);
                if (reservations.Count == 0)
                {
                    return Result<bool>.Success(true);
                }

                foreach (var reservation in reservations)
                {
                    var stockItem = await stockItemRepository.GetByProductAndSkuAsync(
                        reservation.ProductId,
                        reservation.Sku,
                        innerCancellationToken);
                    if (stockItem is not null && stockItem.IsTracked)
                    {
                        var releaseStockResult = stockItem.Release(reservation.Quantity, clock.UtcNow);
                        if (releaseStockResult.IsFailure)
                        {
                            return Result<bool>.Failure(releaseStockResult.Error);
                        }
                    }

                    var releaseReservationResult = reservation.Release(clock.UtcNow);
                    if (releaseReservationResult.IsFailure)
                    {
                        return Result<bool>.Failure(releaseReservationResult.Error);
                    }

                    var movementResult = StockMovement.Create(
                        reservation.ProductId,
                        reservation.Sku,
                        StockMovementType.ReservationReleased,
                        reservation.Quantity,
                        reservation.Id,
                        "Payment failed or cancelled",
                        "system",
                        clock.UtcNow);
                    if (movementResult.IsFailure)
                    {
                        return Result<bool>.Failure(movementResult.Error);
                    }

                    await stockMovementRepository.AddAsync(movementResult.Value, innerCancellationToken);
                }

                await unitOfWork.SaveChangesAsync(innerCancellationToken);
                return Result<bool>.Success(true);
            },
            Math.Max(1, this.options.RetryOnConcurrencyCount),
            cancellationToken);

        return operationResult.IsSuccess
            ? Result.Success()
            : Result.Failure(operationResult.Error);
    }

    private IReadOnlyCollection<InventoryCartLineRequest> NormalizeLines(
        IReadOnlyCollection<InventoryCartLineRequest> lines)
    {
        return lines
            .Where(line => line.ProductId != Guid.Empty && line.Quantity > 0)
            .Select(line => new InventoryCartLineRequest(
                line.ProductId,
                string.IsNullOrWhiteSpace(line.Sku) ? null : line.Sku.Trim(),
                line.Quantity))
            .GroupBy(line => new { line.ProductId, line.Sku })
            .Select(group => new InventoryCartLineRequest(
                group.Key.ProductId,
                group.Key.Sku,
                group.Sum(line => line.Quantity)))
            .ToList();
    }

    private Result<string> NormalizeCartId(string cartId)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            return Result<string>.Failure(new Error(
                "inventory.cart_id.required",
                "Cart id is required."));
        }

        return Result<string>.Success(cartId.Trim());
    }
}
