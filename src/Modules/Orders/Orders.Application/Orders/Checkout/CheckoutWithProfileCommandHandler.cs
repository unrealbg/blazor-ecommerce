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

                var lineData = new List<OrderLineData>(cart.Lines.Count);
                foreach (var line in cart.Lines)
                {
                    var moneyResult = Money.Create(line.Currency, line.UnitAmount);
                    if (moneyResult.IsFailure)
                    {
                        return Result<Guid>.Failure(moneyResult.Error);
                    }

                    lineData.Add(new OrderLineData(line.ProductId, line.Name, moneyResult.Value, line.Quantity));
                }

                var orderResult = Order.Create(
                    customerId,
                    lineData,
                    clock.UtcNow,
                    shippingAddressResult.Value,
                    billingAddressResult.Value);

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
