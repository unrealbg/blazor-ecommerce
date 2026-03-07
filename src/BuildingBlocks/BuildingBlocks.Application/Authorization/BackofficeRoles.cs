namespace BuildingBlocks.Application.Authorization;

public static class BackofficeRoles
{
    public const string Admin = "Admin";
    public const string CatalogManager = "CatalogManager";
    public const string OrderManager = "OrderManager";
    public const string SupportAgent = "SupportAgent";
    public const string ContentManager = "ContentManager";
    public const string InventoryManager = "InventoryManager";
    public const string MarketingManager = "MarketingManager";
    public const string FinanceManager = "FinanceManager";
    public const string ReviewerModerator = "ReviewerModerator";

    public static IReadOnlyCollection<string> DefaultRoles { get; } =
    [
        Admin,
        CatalogManager,
        OrderManager,
        SupportAgent,
        ContentManager,
        InventoryManager,
        MarketingManager,
        FinanceManager,
        ReviewerModerator,
    ];
}
