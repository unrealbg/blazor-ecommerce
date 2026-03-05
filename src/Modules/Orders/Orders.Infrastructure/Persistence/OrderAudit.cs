namespace Orders.Infrastructure.Persistence;

public sealed class OrderAudit
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid OrderId { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTime LoggedOnUtc { get; set; }
}
