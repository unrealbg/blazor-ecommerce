using Inventory.Application.Stock;
using Inventory.Domain.Stock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure.Workers;

internal sealed class ReservationExpirationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<InventoryModuleOptions> options,
    ILogger<ReservationExpirationWorker> logger)
    : BackgroundService
{
    private readonly InventoryModuleOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed processing reservation expiration sweep.");
            }

            await Task.Delay(this.options.ExpirationSweepInterval, stoppingToken);
        }
    }

    private async Task SweepExpiredReservationsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var stockReservationRepository = scope.ServiceProvider.GetRequiredService<IStockReservationRepository>();
        var stockItemRepository = scope.ServiceProvider.GetRequiredService<IStockItemRepository>();
        var stockMovementRepository = scope.ServiceProvider.GetRequiredService<IStockMovementRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IInventoryUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<BuildingBlocks.Domain.Abstractions.IClock>();

        var result = await unitOfWork.ExecuteWithConcurrencyRetryAsync(
            async innerCancellationToken =>
            {
                var expiredReservations = await stockReservationRepository.ListExpiredActiveAsync(
                    clock.UtcNow,
                    200,
                    innerCancellationToken);

                if (expiredReservations.Count == 0)
                {
                    return BuildingBlocks.Domain.Results.Result<bool>.Success(true);
                }

                foreach (var reservation in expiredReservations)
                {
                    var stockItem = await stockItemRepository.GetByVariantIdAsync(
                        reservation.VariantId,
                        innerCancellationToken);

                    if (stockItem is not null && stockItem.IsTracked)
                    {
                        var releaseResult = stockItem.Release(reservation.Quantity, clock.UtcNow);
                        if (releaseResult.IsFailure)
                        {
                            return BuildingBlocks.Domain.Results.Result<bool>.Failure(releaseResult.Error);
                        }
                    }

                    var expireResult = reservation.Expire(clock.UtcNow);
                    if (expireResult.IsFailure)
                    {
                        return BuildingBlocks.Domain.Results.Result<bool>.Failure(expireResult.Error);
                    }

                    var movementResult = StockMovement.Create(
                        reservation.ProductId,
                        reservation.Sku,
                        StockMovementType.ReservationExpired,
                        reservation.Quantity,
                        reservation.Id,
                        "Reservation expired by worker",
                        "system",
                        clock.UtcNow);
                    if (movementResult.IsFailure)
                    {
                        return BuildingBlocks.Domain.Results.Result<bool>.Failure(movementResult.Error);
                    }

                    await stockMovementRepository.AddAsync(movementResult.Value, innerCancellationToken);
                }

                await unitOfWork.SaveChangesAsync(innerCancellationToken);
                logger.LogInformation("Expired {Count} stock reservations.", expiredReservations.Count);

                return BuildingBlocks.Domain.Results.Result<bool>.Success(true);
            },
            Math.Max(1, this.options.RetryOnConcurrencyCount),
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Reservation expiration sweep failed: {Code} - {Message}", result.Error.Code, result.Error.Message);
        }
    }
}
