namespace Backoffice.Application.Backoffice;

public sealed record BackofficeDashboardSummaryDto(
    int OrdersToday,
    int PendingPaymentOrders,
    int OrdersAwaitingFulfillment,
    int LowStockVariants,
    int OutOfStockVariants,
    int ActiveReservations,
    int PendingReviewModeration,
    int FailedPaymentWebhooks,
    int FailedShippingWebhooks,
    int OpenReports,
    int PromotionsExpiringSoon,
    int ContentDraftCount,
    IReadOnlyCollection<BackofficeAuditEntryDto> RecentAuditEntries);
