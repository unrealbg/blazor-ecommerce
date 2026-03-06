using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Orders.Application.Orders;
using Orders.Application.Orders.Checkout;
using Orders.Domain.Orders;

namespace Orders.Tests;

public sealed class CheckoutWithProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_CreateGuestCustomer_WhenUserIsAnonymous()
    {
        var guestCustomerId = Guid.Parse("272e4fed-cbb4-4a8a-9754-9f4121892ecd");
        var customerAccessor = new StubCustomerCheckoutAccessor
        {
            GuestProfile = new CustomerCheckoutProfile(
                guestCustomerId,
                "guest@example.com",
                "Guest",
                "Buyer",
                "+359888000000"),
        };

        var cartAccessor = new StubCartCheckoutAccessor
        {
            Snapshot = BuildCartSnapshot("session-guest"),
        };

        var fixture = CreateHandler(cartAccessor, customerAccessor);

        var result = await fixture.Handler.Handle(
            new CheckoutWithProfileCommand(
                "session-guest",
                "guest@example.com",
                BuildAddress("Guest", "Buyer"),
                BuildAddress("Guest", "Buyer"),
                "guest-idempotency",
                UserId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, customerAccessor.GetByUserIdCalls);
        Assert.Equal(1, customerAccessor.GetOrCreateGuestCalls);
        Assert.NotNull(fixture.OrderRepository.AddedOrder);
        Assert.Equal(guestCustomerId.ToString("N"), fixture.OrderRepository.AddedOrder!.CustomerId);
        Assert.Equal(guestCustomerId, fixture.SessionCache.LastCustomerId);
        Assert.Equal("session-guest", fixture.SessionCache.LastSessionId);
    }

    [Fact]
    public async Task Handle_Should_UseExistingCustomer_WhenUserIsAuthenticated()
    {
        var userId = Guid.Parse("e95af2ed-2ccb-4421-95d0-b370a771c653");
        var customerId = Guid.Parse("244cca3d-a635-4f9c-bc85-f9d30b3f508d");
        var customerAccessor = new StubCustomerCheckoutAccessor
        {
            UserProfile = new CustomerCheckoutProfile(
                customerId,
                "account@example.com",
                "Alice",
                "Buyer",
                "+359888000000"),
        };

        var cartAccessor = new StubCartCheckoutAccessor
        {
            Snapshot = BuildCartSnapshot("session-user"),
        };

        var fixture = CreateHandler(cartAccessor, customerAccessor);

        var result = await fixture.Handler.Handle(
            new CheckoutWithProfileCommand(
                "session-user",
                "other-email@example.com",
                BuildAddress("Alice", "Buyer"),
                BuildAddress("Alice", "Buyer"),
                "user-idempotency",
                userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, customerAccessor.GetByUserIdCalls);
        Assert.Equal(0, customerAccessor.GetOrCreateGuestCalls);
        Assert.NotNull(fixture.OrderRepository.AddedOrder);
        Assert.Equal(customerId.ToString("N"), fixture.OrderRepository.AddedOrder!.CustomerId);
        Assert.Equal(customerId, fixture.SessionCache.LastCustomerId);
        Assert.Equal("session-user", fixture.SessionCache.LastSessionId);
    }

    private static HandlerFixture CreateHandler(
        StubCartCheckoutAccessor cartAccessor,
        StubCustomerCheckoutAccessor customerAccessor)
    {
        var idempotencyRepository = new StubCheckoutIdempotencyRepository();
        var orderRepository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var sessionCache = new StubCustomerSessionCache();
        var handler = new CheckoutWithProfileCommandHandler(
            cartAccessor,
            new StubInventoryReservationService(),
            idempotencyRepository,
            customerAccessor,
            sessionCache,
            orderRepository,
            unitOfWork,
            new StubClock());

        return new HandlerFixture(handler, orderRepository, sessionCache);
    }

    private sealed class StubInventoryReservationService : IInventoryReservationService
    {
        public Task<Result> SyncCartReservationAsync(
            string cartId,
            Guid productId,
            string? sku,
            int quantity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> ReleaseAllCartReservationsAsync(string cartId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result<InventoryReservationValidationResult>> ValidateCartReservationsAsync(
            string cartId,
            IReadOnlyCollection<InventoryCartLineRequest> lines,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<InventoryReservationValidationResult>.Success(
                new InventoryReservationValidationResult(true, [])));
        }

        public Task<Result> ConsumeCartReservationsAsync(
            string cartId,
            Guid orderId,
            IReadOnlyCollection<InventoryCartLineRequest> lines,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success());
        }
    }

    private static CartCheckoutSnapshot BuildCartSnapshot(string sessionId)
    {
        return new CartCheckoutSnapshot(
            Guid.NewGuid(),
            sessionId,
            [new CartCheckoutLineSnapshot(Guid.NewGuid(), "Keyboard", "EUR", 99m, 1)]);
    }

    private static CheckoutAddressInput BuildAddress(string firstName, string lastName)
    {
        return new CheckoutAddressInput(firstName, lastName, "Main street 1", "Sofia", "1000", "BG", "+359888000000");
    }

    private sealed record HandlerFixture(
        CheckoutWithProfileCommandHandler Handler,
        StubOrderRepository OrderRepository,
        StubCustomerSessionCache SessionCache);

    private sealed class StubCartCheckoutAccessor : ICartCheckoutAccessor
    {
        public CartCheckoutSnapshot? Snapshot { get; set; }

        public Guid? ClearedCartId { get; private set; }

        public Task<CartCheckoutSnapshot?> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Snapshot);
        }

        public Task ClearCartAsync(Guid cartId, CancellationToken cancellationToken)
        {
            ClearedCartId = cartId;
            return Task.CompletedTask;
        }
    }

    private sealed class StubCheckoutIdempotencyRepository : ICheckoutIdempotencyRepository
    {
        public Task<CheckoutIdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<CheckoutIdempotencyRecord?>(null);
        }

        public Task AddAsync(
            string idempotencyKey,
            string customerId,
            Guid orderId,
            DateTime createdOnUtc,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubCustomerCheckoutAccessor : ICustomerCheckoutAccessor
    {
        public CustomerCheckoutProfile? GuestProfile { get; init; }

        public CustomerCheckoutProfile? UserProfile { get; init; }

        public int GetByUserIdCalls { get; private set; }

        public int GetOrCreateGuestCalls { get; private set; }

        public Task<CustomerCheckoutProfile> GetOrCreateGuestByEmailAsync(
            string email,
            string? firstName,
            string? lastName,
            string? phoneNumber,
            CancellationToken cancellationToken)
        {
            GetOrCreateGuestCalls++;
            return Task.FromResult(GuestProfile ?? throw new InvalidOperationException("Guest profile was not configured."));
        }

        public Task<CustomerCheckoutProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            GetByUserIdCalls++;
            return Task.FromResult(UserProfile);
        }
    }

    private sealed class StubCustomerSessionCache : ICustomerSessionCache
    {
        public Guid? LastCustomerId { get; private set; }

        public string? LastSessionId { get; private set; }

        public Task TouchCartSessionAsync(string sessionId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task TouchCustomerSessionAsync(
            Guid customerId,
            string sessionId,
            CancellationToken cancellationToken)
        {
            LastCustomerId = customerId;
            LastSessionId = sessionId;
            return Task.CompletedTask;
        }
    }

    private sealed class StubOrderRepository : IOrderRepository
    {
        public Order? AddedOrder { get; private set; }

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            AddedOrder = order;
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Order?>(null);
        }

        public Task<IReadOnlyCollection<Order>> ListByCustomerIdAsync(
            string customerId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Order>>([]);
        }
    }

    private sealed class StubOrdersUnitOfWork : IOrdersUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            return operation(cancellationToken);
        }
    }

    private sealed class StubClock : IClock
    {
        public DateTime UtcNow => new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);
    }
}
