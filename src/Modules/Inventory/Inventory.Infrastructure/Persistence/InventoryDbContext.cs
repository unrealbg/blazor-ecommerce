using BuildingBlocks.Domain.Results;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Inventory.Application.Stock;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer), IInventoryUnitOfWork
{
    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is not null ||
            string.Equals(Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            return await operation(cancellationToken);
        }

        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<TResult>> ExecuteWithConcurrencyRetryAsync<TResult>(
        Func<CancellationToken, Task<Result<TResult>>> operation,
        int retryCount,
        CancellationToken cancellationToken)
    {
        var retries = Math.Max(1, retryCount);

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                return await ExecuteInTransactionAsync(operation, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State != EntityState.Detached))
                {
                    entry.State = EntityState.Detached;
                }

                if (attempt == retries)
                {
                    return Result<TResult>.Failure(new Error(
                        "inventory.stock.concurrency_conflict",
                        "Stock changed during processing. Please retry."));
                }
            }
        }

        return Result<TResult>.Failure(new Error(
            "inventory.stock.concurrency_conflict",
            "Stock changed during processing. Please retry."));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
