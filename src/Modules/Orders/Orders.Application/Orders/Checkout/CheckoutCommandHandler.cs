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
    ICartPricingService cartPricingService,
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

                    var pricingResult = await cartPricingService.PriceAsync(
                        new CartPricingRequest(
                            request.CustomerId,
                            IsAuthenticated: false,
                            cart.Lines
                                .Select(line => new CartPricingLineRequest(line.ProductId, line.VariantId, line.Quantity))
                                .ToList(),
                            cart.AppliedCouponCode,
                            Shipping: null,
                            BypassCache: true,
                            StrictCouponValidation: true),
                        innerCancellationToken);
                    if (pricingResult.IsFailure)
                    {
                        return Result<Guid>.Failure(pricingResult.Error);
                    }

                    var lineDataResult = OrderLinePricingSnapshotFactory.Create(cart.Lines, pricingResult.Value);
                    if (lineDataResult.IsFailure)
                    {
                        return Result<Guid>.Failure(lineDataResult.Error);
                    }

                    var orderResult = Order.Create(
                        cart.CustomerId,
                        cart.CustomerId,
                        lineDataResult.Value,
                        clock.UtcNow,
                        subtotalBeforeDiscountAmount: pricingResult.Value.SubtotalBeforeDiscountAmount,
                        lineDiscountTotalAmount: pricingResult.Value.LineDiscountTotalAmount,
                        cartDiscountTotalAmount: pricingResult.Value.CartDiscountTotalAmount,
                        shippingDiscountTotalAmount: pricingResult.Value.ShippingDiscountTotalAmount,
                        appliedCouponsJson: SerializeCouponCodes(pricingResult.Value.AppliedCouponCode),
                        appliedPromotionsJson: SerializeAppliedPromotions(pricingResult.Value.AppliedDiscounts));
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

    private static string? SerializeCouponCodes(string? couponCode)
    {
        return string.IsNullOrWhiteSpace(couponCode)
            ? null
            : System.Text.Json.JsonSerializer.Serialize(new[] { couponCode.Trim().ToUpperInvariant() });
    }

    private static string? SerializeAppliedPromotions(IReadOnlyCollection<PricingDiscountApplication> discounts)
    {
        return discounts.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(discounts);
    }
}
