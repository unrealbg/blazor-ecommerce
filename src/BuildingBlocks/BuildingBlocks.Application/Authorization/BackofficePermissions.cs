namespace BuildingBlocks.Application.Authorization;

public static class BackofficePermissions
{
    public const string DashboardView = "Dashboard.View";
    public const string CatalogView = "Catalog.View";
    public const string CatalogEdit = "Catalog.Edit";
    public const string CatalogPublish = "Catalog.Publish";
    public const string OrdersView = "Orders.View";
    public const string OrdersEdit = "Orders.Edit";
    public const string OrdersCancel = "Orders.Cancel";
    public const string OrdersRefund = "Orders.Refund";
    public const string CustomersView = "Customers.View";
    public const string CustomersEdit = "Customers.Edit";
    public const string CustomersImpersonationDisabled = "Customers.ImpersonationDisabled";
    public const string InventoryView = "Inventory.View";
    public const string InventoryAdjust = "Inventory.Adjust";
    public const string PaymentsView = "Payments.View";
    public const string PaymentsRefund = "Payments.Refund";
    public const string ShippingView = "Shipping.View";
    public const string ShippingManage = "Shipping.Manage";
    public const string ShippingCreateShipment = "Shipping.CreateShipment";
    public const string PricingView = "Pricing.View";
    public const string PricingEdit = "Pricing.Edit";
    public const string PricingPublish = "Pricing.Publish";
    public const string ReviewsView = "Reviews.View";
    public const string ReviewsModerate = "Reviews.Moderate";
    public const string ContentView = "Content.View";
    public const string ContentEdit = "Content.Edit";
    public const string SystemView = "System.View";
    public const string AuditView = "Audit.View";
    public const string StaffView = "Staff.View";
    public const string StaffEdit = "Staff.Edit";

    public static IReadOnlyCollection<string> All { get; } =
    [
        DashboardView,
        CatalogView,
        CatalogEdit,
        CatalogPublish,
        OrdersView,
        OrdersEdit,
        OrdersCancel,
        OrdersRefund,
        CustomersView,
        CustomersEdit,
        CustomersImpersonationDisabled,
        InventoryView,
        InventoryAdjust,
        PaymentsView,
        PaymentsRefund,
        ShippingView,
        ShippingManage,
        ShippingCreateShipment,
        PricingView,
        PricingEdit,
        PricingPublish,
        ReviewsView,
        ReviewsModerate,
        ContentView,
        ContentEdit,
        SystemView,
        AuditView,
        StaffView,
        StaffEdit,
    ];
}
