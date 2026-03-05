using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;

namespace Catalog.Domain.Products;

public sealed class Product : AggregateRoot<Guid>
{
    private Product()
    {
    }

    private Product(Guid id, string name, Money price, DateTime createdOnUtc)
    {
        Id = id;
        Name = name;
        Price = price;
        CreatedOnUtc = createdOnUtc;
    }

    public string Name { get; private set; } = string.Empty;

    public Money Price { get; private set; } = null!;

    public DateTime CreatedOnUtc { get; private set; }

    public static Result<Product> Create(string name, Money price, DateTime createdOnUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Product>.Failure(
                new Error("catalog.product.name.required", "Product name is required."));
        }

        var trimmedName = name.Trim();
        var product = new Product(Guid.NewGuid(), trimmedName, price, createdOnUtc);

        return Result<Product>.Success(product);
    }
}
