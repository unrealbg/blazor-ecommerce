using Backoffice.Application.Backoffice;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;
using Payments.Domain.Payments;
using Pricing.Domain.Promotions;
using Reviews.Domain.Reports;
using Reviews.Domain.Reviews;
using Shipping.Domain.Shipping;

namespace Backoffice.Infrastructure.Services;

internal sealed partial class BackofficeQueryService
{
    public async Task<BackofficeDashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var utcToday = DateTime.UtcNow.Date;
        var tomorrow = utcToday.AddDays(1);
        var soonThreshold = DateTime.UtcNow.AddDays(7);

        var recentAuditEntries = await GetRecentAuditEntriesAsync(8, cancellationToken);

        return new BackofficeDashboardSummaryDto(
            OrdersToday: await ordersDbContext.Orders.CountAsync(
                order => order.PlacedAtUtc >= utcToday && order.PlacedAtUtc < tomorrow,
                cancellationToken),
            PendingPaymentOrders: await ordersDbContext.Orders.CountAsync(
                order => order.Status == OrderStatus.PendingPayment,
                cancellationToken),
            OrdersAwaitingFulfillment: await ordersDbContext.Orders.CountAsync(
                order =>
                    order.Status != OrderStatus.Cancelled &&
                    order.FulfillmentStatus != OrderFulfillmentStatus.Fulfilled,
                cancellationToken),
            LowStockVariants: await inventoryDbContext.StockItems.CountAsync(
                item =>
                    item.IsTracked &&
                    item.AvailableQuantity > 0 &&
                    item.AvailableQuantity <= DefaultLowStockThreshold,
                cancellationToken),
            OutOfStockVariants: await inventoryDbContext.StockItems.CountAsync(
                item => item.IsTracked && item.AvailableQuantity <= 0,
                cancellationToken),
            ActiveReservations: await inventoryDbContext.StockReservations.CountAsync(
                reservation => reservation.Status == StockReservationStatus.Active,
                cancellationToken),
            PendingReviewModeration: await reviewsDbContext.ProductReviews.CountAsync(
                review => review.Status == ModerationStatus.Pending,
                cancellationToken),
            FailedPaymentWebhooks: await paymentsDbContext.WebhookInboxMessages.CountAsync(
                message => message.ProcessingStatus == WebhookInboxProcessingStatus.Failed,
                cancellationToken),
            FailedShippingWebhooks: await shippingDbContext.CarrierWebhookInboxMessages.CountAsync(
                message => message.ProcessingStatus == CarrierWebhookInboxProcessingStatus.Failed,
                cancellationToken),
            OpenReports: await reviewsDbContext.ReviewReports.CountAsync(
                report => report.Status == ReviewReportStatus.Open,
                cancellationToken),
            PromotionsExpiringSoon: await pricingDbContext.Promotions.CountAsync(
                promotion =>
                    promotion.Status == PromotionStatus.Active &&
                    promotion.EndAtUtc != null &&
                    promotion.EndAtUtc <= soonThreshold,
                cancellationToken),
            ContentDraftCount: 0,
            RecentAuditEntries: recentAuditEntries);
    }
}
