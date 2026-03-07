using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Orders.Application.Orders;
using Orders.Domain.Orders;

namespace Orders.Application.Orders.Checkout;

public sealed class CheckoutCommandHandler(
    ICartCheckoutAccessor cartCheckoutAccessor,
    IInventoryReservationService inventoryReservationService,
    ICheckoutIdempotencyRepository idempotencyRepository,
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CheckoutCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var normalizedKey = request.IdempotencyKey.Trim();

        var existingRecord = await idempotencyRepository.GetByKeyAsync(normalizedKey, cancellationToken);
        if (existingRecord is not null)
        {
            return ResolveExistingRecord(existingRecord, request.CustomerId);
        }

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(
                async innerCancellationToken =>
                {
                    var recordInTransaction = await idempotencyRepository.GetByKeyAsync(
                        normalizedKey,
                        innerCancellationToken);

                    if (recordInTransaction is not null)
                    {
                        return ResolveExistingRecord(recordInTransaction, request.CustomerId);
                    }

                    var cart = await cartCheckoutAccessor.GetByCustomerIdAsync(
                        request.CustomerId,
                        innerCancellationToken);

                    if (cart is null || cart.Lines.Count == 0)
                    {
                        return Result<Guid>.Failure(
                            new Error("orders.checkout.cart_empty", "Cannot checkout an empty cart."));
                    }

                    var normalizedLines = cart.Lines
                        .Select(line => new InventoryCartLineRequest(line.ProductId, line.VariantId, line.Sku, line.Quantity))
                        .ToList();

                    var reservationValidation = await inventoryReservationService.ValidateCartReservationsAsync(
                        cart.CustomerId,
                        normalizedLines,
                        innerCancellationToken);
                    if (reservationValidation.IsFailure)
                    {
                        return Result<Guid>.Failure(reservationValidation.Error);
                    }

                    if (!reservationValidation.Value.IsValid)
                    {
                        return Result<Guid>.Failure(new Error(
                            "orders.checkout.reservation.invalid",
                            "Some cart reservations are invalid or expired. Please refresh your cart."));
                    }

                    var lineData = new List<OrderLineData>(cart.Lines.Count);
                    foreach (var line in cart.Lines)
                    {
                        var moneyResult = Money.Create(line.Currency, line.UnitAmount);
                        if (moneyResult.IsFailure)
                        {
                            return Result<Guid>.Failure(moneyResult.Error);
                        }

                        lineData.Add(new OrderLineData(line.ProductId, line.VariantId, line.Sku, line.ProductName, line.VariantName, line.SelectedOptionsJson, moneyResult.Value, line.Quantity));
                    }

                    var orderResult = Order.Create(
                        cart.CustomerId,
                        cart.CustomerId,
                        lineData,
                        clock.UtcNow);
                    if (orderResult.IsFailure)
                    {
                        return Result<Guid>.Failure(orderResult.Error);
                    }

                    await orderRepository.AddAsync(orderResult.Value, innerCancellationToken);
                    await idempotencyRepository.AddAsync(
                        normalizedKey,
                        request.CustomerId,
                        orderResult.Value.Id,
                        clock.UtcNow,
                        innerCancellationToken);

                    var promoteResult = await inventoryReservationService.PromoteCartReservationsToOrderAsync(
                        cart.CustomerId,
                        orderResult.Value.Id,
                        normalizedLines,
                        innerCancellationToken);
                    if (promoteResult.IsFailure)
                    {
                        return Result<Guid>.Failure(promoteResult.Error);
                    }

                    await cartCheckoutAccessor.ClearCartAsync(cart.CartId, innerCancellationToken);
                    await unitOfWork.SaveChangesAsync(innerCancellationToken);

                    return Result<Guid>.Success(orderResult.Value.Id);
                },
                cancellationToken);
        }
        catch (Exception)
        {
            var recordAfterConflict = await idempotencyRepository.GetByKeyAsync(normalizedKey, cancellationToken);
            if (recordAfterConflict is not null)
            {
                return ResolveExistingRecord(recordAfterConflict, request.CustomerId);
            }

            throw;
        }
    }

    private static Result<Guid> ResolveExistingRecord(
        CheckoutIdempotencyRecord existingRecord,
        string customerId)
    {
        if (!string.Equals(existingRecord.CustomerId, customerId, StringComparison.Ordinal))
        {
            return Result<Guid>.Failure(new Error(
                "orders.checkout.idempotency_key.conflict",
                "Idempotency key has already been used for another customer."));
        }

        return Result<Guid>.Success(existingRecord.OrderId);
    }
}
