using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Payments.Application.Payments;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence;

public sealed class PaymentsDbContext(
    DbContextOptions<PaymentsDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer), IPaymentsUnitOfWork
{
    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<WebhookInboxMessage> WebhookInboxMessages => Set<WebhookInboxMessage>();

    public DbSet<PaymentIdempotencyRecord> PaymentIdempotencyRecords => Set<PaymentIdempotencyRecord>();

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
