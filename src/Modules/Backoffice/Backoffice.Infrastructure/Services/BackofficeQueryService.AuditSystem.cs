using Backoffice.Application.Backoffice;
using Backoffice.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Payments.Domain.Payments;
using Shipping.Domain.Shipping;
using StackExchange.Redis;

namespace Backoffice.Infrastructure.Services;

internal sealed partial class BackofficeQueryService
{
    public async Task<BackofficeAuditPage> GetAuditEntriesAsync(
        string? actor,
        string? actionType,
        string? targetType,
        string? targetId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var query = backofficeDbContext.AuditEntries
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(actor))
        {
            var normalizedActor = actor.Trim().ToLowerInvariant();
            query = query.Where(entry =>
                (entry.ActorEmail != null && entry.ActorEmail.ToLower().Contains(normalizedActor)) ||
                (entry.ActorDisplayName != null && entry.ActorDisplayName.ToLower().Contains(normalizedActor)) ||
                (entry.ActorUserId != null && entry.ActorUserId.ToLower().Contains(normalizedActor)));
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            var normalizedActionType = actionType.Trim().ToLowerInvariant();
            query = query.Where(entry => entry.ActionType.ToLower().Contains(normalizedActionType));
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            var normalizedTargetType = targetType.Trim().ToLowerInvariant();
            query = query.Where(entry => entry.TargetType.ToLower().Contains(normalizedTargetType));
        }

        if (!string.IsNullOrWhiteSpace(targetId))
        {
            var normalizedTargetId = targetId.Trim().ToLowerInvariant();
            query = query.Where(entry => entry.TargetId.ToLower().Contains(normalizedTargetId));
        }

        if (fromUtc is not null)
        {
            query = query.Where(entry => entry.OccurredAtUtc >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            query = query.Where(entry => entry.OccurredAtUtc <= toUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        var items = (await query
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken))
            .Select(MapAuditEntry)
            .ToArray();

        return new BackofficeAuditPage(
            normalizedPage,
            normalizedPageSize,
            totalCount,
            totalPages,
            items);
    }

    public Task<BackofficeAuditEntryDto?> GetAuditEntryAsync(Guid auditEntryId, CancellationToken cancellationToken)
    {
        return backofficeDbContext.AuditEntries
            .AsNoTracking()
            .Where(entry => entry.Id == auditEntryId)
            .ToListAsync(cancellationToken)
            .ContinueWith(
                task => task.Result.Select(MapAuditEntry).SingleOrDefault(),
                cancellationToken);
    }

    public async Task<BackofficeSystemSummaryDto> GetSystemSummaryAsync(CancellationToken cancellationToken)
    {
        var databaseHealthy = await ordersDbContext.Database.CanConnectAsync(cancellationToken) &&
                              await backofficeDbContext.Database.CanConnectAsync(cancellationToken);
        var snapshot = operationalStateRegistry.GetSnapshot();
        var workers = operationalStateRegistry.GetWorkers()
            .Select(worker => new BackofficeWorkerStatusDto(
                worker.Name,
                worker.State,
                worker.LastStartedAtUtc,
                worker.LastSucceededAtUtc,
                worker.LastFailedAtUtc,
                worker.LastError,
                worker.ConsecutiveFailureCount,
                worker.LastDurationMs,
                worker.LastCorrelationId,
                worker.LastProcessedCount,
                worker.LastNote))
            .ToArray();
        var alerts = operationalStateRegistry.GetAlerts()
            .Select(alert => new BackofficeOperationalAlertDto(
                alert.Code,
                alert.Severity,
                alert.Summary,
                alert.Details,
                alert.OccurredAtUtc,
                alert.Context))
            .ToArray();

        return new BackofficeSystemSummaryDto(
            DatabaseHealthy: databaseHealthy,
            RedisHealthy: await CheckRedisAsync(cancellationToken),
            PendingOutboxMessages: snapshot.PendingOutboxMessages,
            FailedOutboxMessages: snapshot.FailedOutboxMessages,
            DeadLetteredOutboxMessages: snapshot.DeadLetteredOutboxMessages,
            OldestPendingOutboxAgeSeconds: snapshot.OldestPendingOutboxAgeSeconds,
            FailedPaymentWebhooks: snapshot.FailedPaymentWebhooks,
            FailedShippingWebhooks: snapshot.FailedShippingWebhooks,
            PendingPaymentWebhooks: snapshot.PendingPaymentWebhooks,
            PendingShippingWebhooks: snapshot.PendingShippingWebhooks,
            SearchDocumentCount: await searchDbContext.ProductSearchDocuments.CountAsync(cancellationToken),
            LowStockVariants: snapshot.LowStockVariants,
            ActiveInventoryReservations: snapshot.ActiveInventoryReservations,
            PendingReviewModeration: snapshot.PendingReviewModeration,
            LastUpdatedAtUtc: snapshot.LastUpdatedAtUtc,
            Workers: workers,
            Alerts: alerts);
    }

    public async Task<IReadOnlyCollection<OrderInternalNoteDto>> GetOrderNotesAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await backofficeDbContext.OrderInternalNotes
            .AsNoTracking()
            .Where(note => note.OrderId == orderId)
            .OrderByDescending(note => note.CreatedAtUtc)
            .Select(note => new OrderInternalNoteDto(
                note.Id,
                note.OrderId,
                note.Note,
                note.CreatedAtUtc,
                note.AuthorUserId,
                note.AuthorEmail,
                note.AuthorDisplayName))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<BackofficeAuditEntryDto>> GetRecentAuditEntriesAsync(
        int take,
        CancellationToken cancellationToken)
    {
        return (await backofficeDbContext.AuditEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken))
            .Select(MapAuditEntry)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<BackofficeAuditEntryDto>> GetOrderAuditEntriesAsync(
        Guid orderId,
        Guid? paymentIntentId,
        Guid? shipmentId,
        CancellationToken cancellationToken)
    {
        var query = backofficeDbContext.AuditEntries
            .AsNoTracking()
            .Where(entry =>
                (entry.TargetType == "Order" && entry.TargetId == orderId.ToString("D")) ||
                (paymentIntentId != null && entry.TargetType == "PaymentIntent" && entry.TargetId == paymentIntentId.Value.ToString("D")) ||
                (shipmentId != null && entry.TargetType == "Shipment" && entry.TargetId == shipmentId.Value.ToString("D")));

        return (await query
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken))
            .Select(MapAuditEntry)
            .ToArray();
    }

    private async Task<bool> CheckRedisAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration["ConnectionStrings:Redis"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            await using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
            var database = connection.GetDatabase();
            await database.PingAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }
        catch (RedisConnectionException)
        {
            return false;
        }
        catch (RedisTimeoutException)
        {
            return false;
        }
    }
}
