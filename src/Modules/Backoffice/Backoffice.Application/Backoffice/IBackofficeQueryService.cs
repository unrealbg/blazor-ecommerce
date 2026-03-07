using System.Security.Claims;

namespace Backoffice.Application.Backoffice;

public interface IBackofficeQueryService
{
    Task<BackofficeSessionDto?> GetSessionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<BackofficeDashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken);

    Task<BackofficeOrderPage> GetOrdersAsync(
        string? orderId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? status,
        string? paymentStatus,
        string? fulfillmentStatus,
        string? customerEmail,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<BackofficeOrderDetailDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<BackofficeCustomerPage> GetCustomersAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<BackofficeCustomerDetailDto?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    Task<BackofficeAuditPage> GetAuditEntriesAsync(
        string? actor,
        string? actionType,
        string? targetType,
        string? targetId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<BackofficeAuditEntryDto?> GetAuditEntryAsync(Guid auditEntryId, CancellationToken cancellationToken);

    Task<BackofficeSystemSummaryDto> GetSystemSummaryAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<OrderInternalNoteDto>> GetOrderNotesAsync(Guid orderId, CancellationToken cancellationToken);
}
