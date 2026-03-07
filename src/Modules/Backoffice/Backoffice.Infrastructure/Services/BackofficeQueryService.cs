using System.Security.Claims;
using Backoffice.Application.Backoffice;
using Backoffice.Infrastructure.Persistence;
using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Infrastructure.Persistence;
using Customers.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Orders.Infrastructure.Persistence;
using Payments.Infrastructure.Persistence;
using Pricing.Infrastructure.Persistence;
using Reviews.Infrastructure.Persistence;
using Search.Infrastructure.Persistence;
using Shipping.Infrastructure.Persistence;

namespace Backoffice.Infrastructure.Services;

internal sealed partial class BackofficeQueryService : IBackofficeQueryService
{
    private const int DefaultLowStockThreshold = 5;

    private readonly IBackofficePermissionService permissionService;
    private readonly BackofficeDbContext backofficeDbContext;
    private readonly OrdersDbContext ordersDbContext;
    private readonly CustomersDbContext customersDbContext;
    private readonly InventoryDbContext inventoryDbContext;
    private readonly PaymentsDbContext paymentsDbContext;
    private readonly PricingDbContext pricingDbContext;
    private readonly ReviewsDbContext reviewsDbContext;
    private readonly ShippingDbContext shippingDbContext;
    private readonly SearchDbContext searchDbContext;
    private readonly OutboxDbContext outboxDbContext;
    private readonly IConfiguration configuration;

    public BackofficeQueryService(
        IBackofficePermissionService permissionService,
        BackofficeDbContext backofficeDbContext,
        OrdersDbContext ordersDbContext,
        CustomersDbContext customersDbContext,
        InventoryDbContext inventoryDbContext,
        PaymentsDbContext paymentsDbContext,
        PricingDbContext pricingDbContext,
        ReviewsDbContext reviewsDbContext,
        ShippingDbContext shippingDbContext,
        SearchDbContext searchDbContext,
        OutboxDbContext outboxDbContext,
        IConfiguration configuration)
    {
        this.permissionService = permissionService;
        this.backofficeDbContext = backofficeDbContext;
        this.ordersDbContext = ordersDbContext;
        this.customersDbContext = customersDbContext;
        this.inventoryDbContext = inventoryDbContext;
        this.paymentsDbContext = paymentsDbContext;
        this.pricingDbContext = pricingDbContext;
        this.reviewsDbContext = reviewsDbContext;
        this.shippingDbContext = shippingDbContext;
        this.searchDbContext = searchDbContext;
        this.outboxDbContext = outboxDbContext;
        this.configuration = configuration;
    }

    public async Task<BackofficeSessionDto?> GetSessionAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var snapshot = await permissionService.GetCurrentStaffSnapshotAsync(principal, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        return new BackofficeSessionDto(
            snapshot.UserId,
            snapshot.Email,
            snapshot.DisplayName,
            snapshot.Department,
            snapshot.IsActive,
            snapshot.LastLoginAtUtc,
            snapshot.Roles,
            snapshot.Permissions);
    }
}
