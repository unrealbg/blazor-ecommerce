using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Orders.Application.Orders;
using Orders.Application.Orders.Checkout;
using Orders.Domain.Orders;

namespace Orders.Tests;

public sealed class CheckoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnFailure_When_CartMissing()
    {
        var cartAccessor = new StubCartCheckoutAccessor();
        var idempotencyRepository = new StubCheckoutIdempotencyRepository();
        var repository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var handler = CreateHandler(cartAccessor, idempotencyRepository, repository, unitOfWork);

        var result = await handler.Handle(new CheckoutCommand("customer-1", "idem-key-1"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(repository.AddedOrder);
        Assert.Null(cartAccessor.ClearedCartId);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_CartHasNoLines()
    {
        var cartAccessor = new StubCartCheckoutAccessor
        {
            Snapshot = new CartCheckoutSnapshot(Guid.NewGuid(), "customer-1", []),
        };

        var idempotencyRepository = new StubCheckoutIdempotencyRepository();
        var repository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var handler = CreateHandler(cartAccessor, idempotencyRepository, repository, unitOfWork);

        var result = await handler.Handle(new CheckoutCommand("customer-1", "idem-key-2"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(repository.AddedOrder);
        Assert.Null(cartAccessor.ClearedCartId);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_Should_CreateOrder_And_ClearCart()
    {
        var cartId = Guid.NewGuid();
        var cartAccessor = new StubCartCheckoutAccessor
        {
            Snapshot = new CartCheckoutSnapshot(
                cartId,
                "customer-1",
                [
                    new CartCheckoutLineSnapshot(Guid.NewGuid(), "Item A", "USD", 10m, 2),
                    new CartCheckoutLineSnapshot(Guid.NewGuid(), "Item B", "USD", 5.25m, 1),
                ]),
        };

        var idempotencyRepository = new StubCheckoutIdempotencyRepository();
        var repository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var handler = CreateHandler(cartAccessor, idempotencyRepository, repository, unitOfWork);

        var result = await handler.Handle(new CheckoutCommand("customer-1", "idem-key-3"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedOrder);
        Assert.Equal(cartId, cartAccessor.ClearedCartId);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal(25.25m, repository.AddedOrder!.Subtotal.Amount);
        Assert.Equal(25.25m, repository.AddedOrder.Total.Amount);
        Assert.Equal(OrderStatus.PendingPayment, repository.AddedOrder.Status);
        Assert.Equal(repository.AddedOrder.Id, idempotencyRepository.LastAdded?.OrderId);
    }

    [Fact]
    public async Task Handle_Should_NotRaiseOrderPlacedDomainEvent_BeforePayment()
    {
        var cartAccessor = new StubCartCheckoutAccessor
        {
            Snapshot = new CartCheckoutSnapshot(
                Guid.NewGuid(),
                "customer-1",
                [new CartCheckoutLineSnapshot(Guid.NewGuid(), "Item A", "USD", 10m, 1)]),
        };

        var idempotencyRepository = new StubCheckoutIdempotencyRepository();
        var repository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var handler = CreateHandler(cartAccessor, idempotencyRepository, repository, unitOfWork);

        var result = await handler.Handle(new CheckoutCommand("customer-1", "idem-key-4"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedOrder);
        Assert.Empty(repository.AddedOrder!.DomainEvents);
    }

    [Fact]
    public async Task Handle_Should_ReturnStoredOrder_When_IdempotencyKeyAlreadyProcessed()
    {
        var existingOrderId = Guid.NewGuid();
        var cartAccessor = new StubCartCheckoutAccessor();
        var idempotencyRepository = new StubCheckoutIdempotencyRepository
        {
            ExistingRecord = new CheckoutIdempotencyRecord("idem-key-5", "customer-1", existingOrderId),
        };

        var repository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var handler = CreateHandler(cartAccessor, idempotencyRepository, repository, unitOfWork);

        var result = await handler.Handle(new CheckoutCommand("customer-1", "idem-key-5"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingOrderId, result.Value);
        Assert.Null(repository.AddedOrder);
        Assert.Null(cartAccessor.ClearedCartId);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_IdempotencyKeyBelongsToDifferentCustomer()
    {
        var idempotencyRepository = new StubCheckoutIdempotencyRepository
        {
            ExistingRecord = new CheckoutIdempotencyRecord("idem-key-6", "customer-2", Guid.NewGuid()),
        };

        var handler = CreateHandler(
            new StubCartCheckoutAccessor(),
            idempotencyRepository,
            new StubOrderRepository(),
            new StubOrdersUnitOfWork());

        var result = await handler.Handle(new CheckoutCommand("customer-1", "idem-key-6"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("orders.checkout.idempotency_key.conflict", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_IdempotencyKeyBelongsToDifferentCustomer_DuringTransactionCheck()
    {
        var cartAccessor = new StubCartCheckoutAccessor();
        var idempotencyRepository = new StubCheckoutIdempotencyRepository
        {
            RecordResolver = (idempotencyKey, call) => call == 2
                ? new CheckoutIdempotencyRecord(idempotencyKey, "customer-2", Guid.NewGuid())
                : null,
        };

        var repository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var handler = CreateHandler(cartAccessor, idempotencyRepository, repository, unitOfWork);

        var result = await handler.Handle(new CheckoutCommand("customer-1", "idem-key-7"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("orders.checkout.idempotency_key.conflict", result.Error.Code);
        Assert.Null(repository.AddedOrder);
        Assert.Null(cartAccessor.ClearedCartId);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_CurrenciesMismatch()
    {
        var order = Order.Create(
            "customer-1",
            "checkout-session-1",
            [
                new OrderLineData(Guid.NewGuid(), "Item A", BuildingBlocks.Domain.Shared.Money.Create("USD", 10m).Value, 1),
                new OrderLineData(Guid.NewGuid(), "Item B", BuildingBlocks.Domain.Shared.Money.Create("EUR", 10m).Value, 1),
            ],
            DateTime.UtcNow);

        Assert.True(order.IsFailure);
    }

    private static CheckoutCommandHandler CreateHandler(
        StubCartCheckoutAccessor cartAccessor,
        StubCheckoutIdempotencyRepository idempotencyRepository,
        StubOrderRepository orderRepository,
        StubOrdersUnitOfWork unitOfWork)
    {
        return new CheckoutCommandHandler(
            cartAccessor,
            new StubInventoryReservationService(),
            idempotencyRepository,
            orderRepository,
            unitOfWork,
            new StubClock());
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

        public Task<Result> PromoteCartReservationsToOrderAsync(
            string cartId,
            Guid orderId,
            IReadOnlyCollection<InventoryCartLineRequest> lines,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> ConsumeOrderReservationsAsync(
            Guid orderId,
            IReadOnlyCollection<InventoryCartLineRequest> lines,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> ReleaseOrderReservationsAsync(Guid orderId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success());
        }
    }

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
        private int getByKeyCalls;

        public CheckoutIdempotencyRecord? ExistingRecord { get; set; }

        public Func<string, int, CheckoutIdempotencyRecord?>? RecordResolver { get; set; }

        public CheckoutIdempotencyRecord? LastAdded { get; private set; }

        public Task<CheckoutIdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
        {
            getByKeyCalls++;
            if (RecordResolver is not null)
            {
                return Task.FromResult<CheckoutIdempotencyRecord?>(RecordResolver(idempotencyKey, getByKeyCalls));
            }

            if (ExistingRecord is null)
            {
                return Task.FromResult<CheckoutIdempotencyRecord?>(null);
            }

            if (!string.Equals(ExistingRecord.IdempotencyKey, idempotencyKey, StringComparison.Ordinal))
            {
                return Task.FromResult<CheckoutIdempotencyRecord?>(null);
            }

            return Task.FromResult<CheckoutIdempotencyRecord?>(ExistingRecord);
        }

        public Task AddAsync(
            string idempotencyKey,
            string customerId,
            Guid orderId,
            DateTime createdOnUtc,
            CancellationToken cancellationToken)
        {
            LastAdded = new CheckoutIdempotencyRecord(idempotencyKey, customerId, orderId);
            ExistingRecord = LastAdded;
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
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
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
        public DateTime UtcNow => new(2026, 3, 5, 12, 0, 0, DateTimeKind.Utc);
    }
}
