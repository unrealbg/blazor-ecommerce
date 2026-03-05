using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using Orders.Application.Orders;
using Orders.Application.Orders.Checkout;
using Orders.Domain.Events;
using Orders.Domain.Orders;

namespace Orders.Tests;

public sealed class CheckoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnFailure_When_CartMissing()
    {
        var cartAccessor = new StubCartCheckoutAccessor();
        var repository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var handler = CreateHandler(cartAccessor, repository, unitOfWork);

        var result = await handler.Handle(new CheckoutCommand("customer-1"), CancellationToken.None);

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

        var repository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var handler = CreateHandler(cartAccessor, repository, unitOfWork);

        var result = await handler.Handle(new CheckoutCommand("customer-1"), CancellationToken.None);

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

        var repository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var handler = CreateHandler(cartAccessor, repository, unitOfWork);

        var result = await handler.Handle(new CheckoutCommand("customer-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedOrder);
        Assert.Equal(cartId, cartAccessor.ClearedCartId);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal(25.25m, repository.AddedOrder!.Subtotal.Amount);
        Assert.Equal(25.25m, repository.AddedOrder.Total.Amount);
        Assert.Equal(OrderStatus.Placed, repository.AddedOrder.Status);
    }

    [Fact]
    public async Task Handle_Should_RaiseOrderPlacedDomainEvent()
    {
        var cartAccessor = new StubCartCheckoutAccessor
        {
            Snapshot = new CartCheckoutSnapshot(
                Guid.NewGuid(),
                "customer-1",
                [new CartCheckoutLineSnapshot(Guid.NewGuid(), "Item A", "USD", 10m, 1)]),
        };

        var repository = new StubOrderRepository();
        var unitOfWork = new StubOrdersUnitOfWork();
        var handler = CreateHandler(cartAccessor, repository, unitOfWork);

        var result = await handler.Handle(new CheckoutCommand("customer-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedOrder);
        Assert.Single(repository.AddedOrder!.DomainEvents);
        Assert.IsType<OrderPlaced>(repository.AddedOrder.DomainEvents.Single());
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_CurrenciesMismatch()
    {
        var order = Order.Create(
            "customer-1",
            [
                new OrderLineData(Guid.NewGuid(), "Item A", BuildingBlocks.Domain.Shared.Money.Create("USD", 10m).Value, 1),
                new OrderLineData(Guid.NewGuid(), "Item B", BuildingBlocks.Domain.Shared.Money.Create("EUR", 10m).Value, 1),
            ],
            DateTime.UtcNow);

        Assert.True(order.IsFailure);
    }

    private static CheckoutCommandHandler CreateHandler(
        StubCartCheckoutAccessor cartAccessor,
        StubOrderRepository orderRepository,
        StubOrdersUnitOfWork unitOfWork)
    {
        return new CheckoutCommandHandler(
            cartAccessor,
            orderRepository,
            unitOfWork,
            new StubClock());
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
