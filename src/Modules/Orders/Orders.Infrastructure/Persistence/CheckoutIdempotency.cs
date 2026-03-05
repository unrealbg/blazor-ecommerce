namespace Orders.Infrastructure.Persistence;

public sealed class CheckoutIdempotency
{
    public Guid Id { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public DateTime CreatedOnUtc { get; set; }
}
