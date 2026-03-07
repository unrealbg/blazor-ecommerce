using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Orders.Application.Orders;
using Orders.Domain.Orders;

namespace Orders.Application.Orders.Checkout;

public sealed class CheckoutWithProfileCommandHandler(
    ICartCheckoutAccessor cartCheckoutAccessor,
    IInventoryReservationService inventoryReservationService,
    IShippingQuoteService shippingQuoteService,
    ICartPricingService cartPricingService,
    ICheckoutIdempotencyRepository idempotencyRepository,
    ICustomerCheckoutAccessor customerCheckoutAccessor,
    ICustomerSessionCache customerSessionCache,
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CheckoutWithProfileCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CheckoutWithProfileCommand request, CancellationToken cancellationToken)
    {
        var normalizedIdempotencyKey = request.IdempotencyKey.Trim();
        var normalizedCartSessionId = request.CartSessionId.Trim();

        var checkoutCustomer = request.UserId is not null
            ? await customerCheckoutAccessor.GetByUserIdAsync(request.UserId.Value, cancellationToken)
            : null;

        checkoutCustomer ??= await customerCheckoutAccessor.GetOrCreateGuestByEmailAsync(
            request.Email,
            request.ShippingAddress.FirstName,
            request.ShippingAddress.LastName,
            request.ShippingAddress.Phone,
            cancellationToken);

        var customerId = checkoutCustomer.CustomerId.ToString("N");

        var existingRecord = await idempotencyRepository.GetByKeyAsync(normalizedIdempotencyKey, cancellationToken);
        if (existingRecord is not null)
        {
            return ResolveExistingRecord(existingRecord, customerId);
        }

        var shippingAddressResult = OrderAddressSnapshot.Create(
            request.ShippingAddress.FirstName,
            request.ShippingAddress.LastName,
            request.ShippingAddress.Street,
            request.ShippingAddress.City,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Country,
            request.ShippingAddress.Phone);

        if (shippingAddressResult.IsFailure)
        {
            return Result<Guid>.Failure(shippingAddressResult.Error);
        }

        var billingAddressResult = OrderAddressSnapshot.Create(
            request.BillingAddress.FirstName,
            request.BillingAddress.LastName,
            request.BillingAddress.Street,
            request.BillingAddress.City,
            request.BillingAddress.PostalCode,
            request.BillingAddress.Country,
            request.BillingAddress.Phone);

        if (billingAddressResult.IsFailure)
        {
            return Result<Guid>.Failure(billingAddressResult.Error);
        }

        return await unitOfWork.ExecuteInTransactionAsync(
            async innerCancellationToken =>
            {
                var existingInTransaction = await idempotencyRepository.GetByKeyAsync(
                    normalizedIdempotencyKey,
                    innerCancellationToken);
                if (existingInTransaction is not null)
                {
                    return ResolveExistingRecord(existingInTransaction, customerId);
                }

                var cart = await cartCheckoutAccessor.GetByCustomerIdAsync(
                    normalizedCartSessionId,
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
                    normalizedCartSessionId,
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

                decimal subtotalAmount = 0m;
                string? orderCurrency = null;
                foreach (var line in cart.Lines)
                {
                    var moneyResult = Money.Create(line.Currency, line.UnitAmount);
                    if (moneyResult.IsFailure)
                    {
                        return Result<Guid>.Failure(moneyResult.Error);
                    }

                    orderCurrency ??= moneyResult.Value.Currency;
                    if (!string.Equals(orderCurrency, moneyResult.Value.Currency, StringComparison.Ordinal))
                    {
                        return Result<Guid>.Failure(new Error(
                            "orders.currency.mismatch",
                            "All cart lines must use the same currency."));
                    }

                    subtotalAmount += Money.Round(moneyResult.Value.Amount * line.Quantity);
                }

                var quoteResult = await shippingQuoteService.ResolveQuoteAsync(
                    request.ShippingAddress.Country,
                    subtotalAmount,
                    orderCurrency ?? "EUR",
                    request.ShippingMethodCode,
                    innerCancellationToken);
                if (quoteResult.IsFailure)
                {
                    return Result<Guid>.Failure(quoteResult.Error);
                }

                var selectedQuote = quoteResult.Value;
                var pricingResult = await cartPricingService.PriceAsync(
                    new CartPricingRequest(
                        customerId,
                        request.UserId is not null,
                        cart.Lines
                            .Select(line => new CartPricingLineRequest(line.ProductId, line.VariantId, line.Quantity))
                            .ToList(),
                        cart.AppliedCouponCode,
                        new ShippingPriceSelection(
                            selectedQuote.ShippingMethodCode,
                            selectedQuote.Currency,
                            selectedQuote.PriceAmount),
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
                    customerId,
                    normalizedCartSessionId,
                    lineDataResult.Value,
                    clock.UtcNow,
                    shippingAddressResult.Value,
                    billingAddressResult.Value,
                    selectedQuote.ShippingMethodCode,
                    selectedQuote.ShippingMethodName,
                    selectedQuote.PriceAmount,
                    selectedQuote.Currency,
                    pricingResult.Value.SubtotalBeforeDiscountAmount,
                    pricingResult.Value.LineDiscountTotalAmount,
                    pricingResult.Value.CartDiscountTotalAmount,
                    pricingResult.Value.ShippingDiscountTotalAmount,
                    SerializeCouponCodes(pricingResult.Value.AppliedCouponCode),
                    SerializeAppliedPromotions(pricingResult.Value.AppliedDiscounts));

                if (orderResult.IsFailure)
                {
                    return Result<Guid>.Failure(orderResult.Error);
                }

                await orderRepository.AddAsync(orderResult.Value, innerCancellationToken);
                await idempotencyRepository.AddAsync(
                    normalizedIdempotencyKey,
                    customerId,
                    orderResult.Value.Id,
                    clock.UtcNow,
                    innerCancellationToken);

                var promoteResult = await inventoryReservationService.PromoteCartReservationsToOrderAsync(
                    normalizedCartSessionId,
                    orderResult.Value.Id,
                    normalizedLines,
                    innerCancellationToken);
                if (promoteResult.IsFailure)
                {
                    return Result<Guid>.Failure(promoteResult.Error);
                }

                await cartCheckoutAccessor.ClearCartAsync(cart.CartId, innerCancellationToken);
                await unitOfWork.SaveChangesAsync(innerCancellationToken);

                await customerSessionCache.TouchCustomerSessionAsync(
                    checkoutCustomer.CustomerId,
                    normalizedCartSessionId,
                    innerCancellationToken);

                return Result<Guid>.Success(orderResult.Value.Id);
            },
            cancellationToken);
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
