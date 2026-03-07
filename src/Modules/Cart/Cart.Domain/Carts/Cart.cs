using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;

namespace Cart.Domain.Carts;

public sealed class Cart : AggregateRoot<Guid>
{
    private readonly List<CartLine> _lines = [];

    private Cart()
    {
    }

    private Cart(Guid id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
        RowVersion = 0L;
    }

    public string CustomerId { get; private set; } = string.Empty;

    public string? AppliedCouponCode { get; private set; }

    public long RowVersion { get; private set; }

    public IReadOnlyCollection<CartLine> Lines => _lines.AsReadOnly();

    public static Result<Cart> Create(string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Result<Cart>.Failure(new Error("cart.customer.required", "Customer id is required."));
        }

        return Result<Cart>.Success(new Cart(Guid.NewGuid(), customerId.Trim()));
    }

    public Result AddItem(
        Guid productId,
        Guid variantId,
        string? sku,
        string productName,
        string? variantName,
        string? selectedOptionsJson,
        string? imageUrl,
        Money unitPrice,
        int quantity)
    {
        if (productId == Guid.Empty)
        {
            return Result.Failure(new Error("cart.item.product.required", "Product id is required."));
        }

        if (variantId == Guid.Empty)
        {
            return Result.Failure(new Error("cart.item.variant.required", "Variant id is required."));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            return Result.Failure(new Error("cart.item.name.required", "Product name is required."));
        }

        if (quantity <= 0)
        {
            return Result.Failure(new Error("cart.item.quantity.invalid", "Quantity must be greater than zero."));
        }

        var existingLine = _lines.FirstOrDefault(line => line.VariantId == variantId);
        if (existingLine is not null)
        {
            existingLine.IncreaseQuantity(quantity);
            IncrementRowVersion();
            return Result.Success();
        }

        _lines.Add(CartLine.Create(
            productId,
            variantId,
            sku,
            productName.Trim(),
            string.IsNullOrWhiteSpace(variantName) ? null : variantName.Trim(),
            string.IsNullOrWhiteSpace(selectedOptionsJson) ? null : selectedOptionsJson.Trim(),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            unitPrice,
            quantity));
        IncrementRowVersion();
        return Result.Success();
    }

    public Result UpdateItemQuantity(Guid variantId, int quantity)
    {
        if (variantId == Guid.Empty)
        {
            return Result.Failure(new Error("cart.item.variant.required", "Variant id is required."));
        }

        if (quantity <= 0)
        {
            return Result.Failure(new Error("cart.item.quantity.invalid", "Quantity must be greater than zero."));
        }

        var existingLine = _lines.FirstOrDefault(line => line.VariantId == variantId);
        if (existingLine is null)
        {
            return Result.Failure(new Error("cart.item.not_found", "Cart item was not found."));
        }

        existingLine.SetQuantity(quantity);
        IncrementRowVersion();
        return Result.Success();
    }

    public Result RemoveItem(Guid variantId)
    {
        if (variantId == Guid.Empty)
        {
            return Result.Failure(new Error("cart.item.variant.required", "Variant id is required."));
        }

        var existingLine = _lines.FirstOrDefault(line => line.VariantId == variantId);
        if (existingLine is null)
        {
            return Result.Failure(new Error("cart.item.not_found", "Cart item was not found."));
        }

        _lines.Remove(existingLine);
        IncrementRowVersion();
        return Result.Success();
    }

    public Result ApplyCoupon(string couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return Result.Failure(new Error("cart.coupon.required", "Coupon code is required."));
        }

        AppliedCouponCode = couponCode.Trim().ToUpperInvariant();
        IncrementRowVersion();
        return Result.Success();
    }

    public void RemoveCoupon()
    {
        AppliedCouponCode = null;
        IncrementRowVersion();
    }

    public void Clear()
    {
        _lines.Clear();
        AppliedCouponCode = null;
        IncrementRowVersion();
    }

    private void IncrementRowVersion()
    {
        RowVersion++;
    }
}
