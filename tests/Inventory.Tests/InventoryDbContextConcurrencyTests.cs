using BuildingBlocks.Domain.Results;
using BuildingBlocks.Infrastructure.Messaging;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Tests;

public sealed class InventoryDbContextConcurrencyTests
{
    [Fact]
    public async Task ExecuteWithConcurrencyRetryAsync_Should_RetryAndSucceed()
    {
        await using var dbContext = CreateDbContext();
        var attempts = 0;

        var result = await dbContext.ExecuteWithConcurrencyRetryAsync(
            _ =>
            {
                attempts++;
                if (attempts == 1)
                {
                    throw new DbUpdateConcurrencyException("Simulated conflict.");
                }

                return Task.FromResult(Result<int>.Success(42));
            },
            retryCount: 3,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteWithConcurrencyRetryAsync_Should_ReturnFailure_WhenRetryLimitIsReached()
    {
        await using var dbContext = CreateDbContext();
        var attempts = 0;

        var result = await dbContext.ExecuteWithConcurrencyRetryAsync<int>(
            _ =>
            {
                attempts++;
                throw new DbUpdateConcurrencyException("Simulated conflict.");
            },
            retryCount: 2,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("inventory.stock.concurrency_conflict", result.Error.Code);
        Assert.Equal(2, attempts);
    }

    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"inventory-concurrency-{Guid.NewGuid():N}")
            .Options;

        return new InventoryDbContext(options, new SystemTextJsonEventSerializer());
    }
}
