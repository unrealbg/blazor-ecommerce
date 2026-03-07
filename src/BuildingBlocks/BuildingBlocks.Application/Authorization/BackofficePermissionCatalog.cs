namespace BuildingBlocks.Application.Authorization;

public static class BackofficePermissionCatalog
{
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> DefaultRolePermissions { get; } =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [BackofficeRoles.Admin] = BackofficePermissions.All
                .Where(permission => !string.Equals(
                    permission,
                    BackofficePermissions.CustomersImpersonationDisabled,
                    StringComparison.Ordinal))
                .ToArray(),
            [BackofficeRoles.CatalogManager] =
            [
                BackofficePermissions.DashboardView,
                BackofficePermissions.CatalogView,
                BackofficePermissions.CatalogEdit,
                BackofficePermissions.CatalogPublish,
                BackofficePermissions.InventoryView,
                BackofficePermissions.PricingView,
                BackofficePermissions.ContentView,
            ],
            [BackofficeRoles.OrderManager] =
            [
                BackofficePermissions.DashboardView,
                BackofficePermissions.OrdersView,
                BackofficePermissions.OrdersEdit,
                BackofficePermissions.OrdersCancel,
                BackofficePermissions.OrdersRefund,
                BackofficePermissions.PaymentsView,
                BackofficePermissions.PaymentsRefund,
                BackofficePermissions.ShippingView,
                BackofficePermissions.ShippingManage,
                BackofficePermissions.ShippingCreateShipment,
                BackofficePermissions.CustomersView,
                BackofficePermissions.AuditView,
            ],
            [BackofficeRoles.SupportAgent] =
            [
                BackofficePermissions.DashboardView,
                BackofficePermissions.OrdersView,
                BackofficePermissions.CustomersView,
                BackofficePermissions.CustomersEdit,
                BackofficePermissions.ReviewsView,
                BackofficePermissions.ContentView,
            ],
            [BackofficeRoles.ContentManager] =
            [
                BackofficePermissions.DashboardView,
                BackofficePermissions.ContentView,
                BackofficePermissions.ContentEdit,
                BackofficePermissions.CatalogView,
                BackofficePermissions.CatalogPublish,
            ],
            [BackofficeRoles.InventoryManager] =
            [
                BackofficePermissions.DashboardView,
                BackofficePermissions.InventoryView,
                BackofficePermissions.InventoryAdjust,
                BackofficePermissions.OrdersView,
                BackofficePermissions.ShippingView,
            ],
            [BackofficeRoles.MarketingManager] =
            [
                BackofficePermissions.DashboardView,
                BackofficePermissions.PricingView,
                BackofficePermissions.PricingEdit,
                BackofficePermissions.PricingPublish,
                BackofficePermissions.CatalogView,
                BackofficePermissions.ContentView,
                BackofficePermissions.ContentEdit,
                BackofficePermissions.ReviewsView,
            ],
            [BackofficeRoles.FinanceManager] =
            [
                BackofficePermissions.DashboardView,
                BackofficePermissions.OrdersView,
                BackofficePermissions.PaymentsView,
                BackofficePermissions.PaymentsRefund,
                BackofficePermissions.OrdersRefund,
                BackofficePermissions.AuditView,
                BackofficePermissions.SystemView,
            ],
            [BackofficeRoles.ReviewerModerator] =
            [
                BackofficePermissions.DashboardView,
                BackofficePermissions.ReviewsView,
                BackofficePermissions.ReviewsModerate,
                BackofficePermissions.CatalogView,
            ],
        };

    public static bool IsKnownPermission(string permission)
    {
        return BackofficePermissions.All.Contains(permission, StringComparer.Ordinal);
    }
}
