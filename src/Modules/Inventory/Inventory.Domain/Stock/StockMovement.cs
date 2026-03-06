using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Inventory.Domain.Stock;

public sealed class StockMovement : Entity<Guid>
{
    private StockMovement()
    {
    }

    private StockMovement(
        Guid id,
        Guid productId,
        string? sku,
        StockMovementType type,
        int quantityDelta,
        Guid? referenceId,
        string? reason,
        string? createdBy,
        DateTime createdAtUtc)
    {
        Id = id;
        ProductId = productId;
        Sku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
        Type = type;
        QuantityDelta = quantityDelta;
        ReferenceId = referenceId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? null : createdBy.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ProductId { get; private set; }

    public string? Sku { get; private set; }

    public StockMovementType Type { get; private set; }

    public int QuantityDelta { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public string? Reason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public static Result<StockMovement> Create(
        Guid productId,
        string? sku,
        StockMovementType type,
        int quantityDelta,
        Guid? referenceId,
        string? reason,
        string? createdBy,
        DateTime createdAtUtc)
    {
        if (productId == Guid.Empty)
        {
            return Result<StockMovement>.Failure(
                new Error("inventory.stock_movement.product_id.required", "Product id is required."));
        }

        if (quantityDelta == 0)
        {
            return Result<StockMovement>.Failure(
                new Error("inventory.stock_movement.quantity.invalid", "Quantity delta must be non-zero."));
        }

        return Result<StockMovement>.Success(new StockMovement(
            Guid.NewGuid(),
            productId,
            sku,
            type,
            quantityDelta,
            referenceId,
            reason,
            createdBy,
            createdAtUtc));
    }
}
