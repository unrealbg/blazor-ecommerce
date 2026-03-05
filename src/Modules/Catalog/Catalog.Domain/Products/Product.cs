using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;

namespace Catalog.Domain.Products;

public sealed class Product : AggregateRoot<Guid>
{
    private Product()
    {
    }

    private Product(Guid id, string name, string? description, Money price, bool isActive)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        IsActive = isActive;
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Money Price { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public static Result<Product> Create(string name, string? description, Money price, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Product>.Failure(
                new Error("catalog.product.name.required", "Product name is required."));
        }

        var trimmedName = name.Trim();
        var normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        if (normalizedDescription is { Length: > 2000 })
        {
            return Result<Product>.Failure(
                new Error("catalog.product.description.too_long", "Product description is too long."));
        }

        var product = new Product(Guid.NewGuid(), trimmedName, normalizedDescription, price, isActive);

        return Result<Product>.Success(product);
    }
}
