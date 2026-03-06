using BuildingBlocks.Domain.Primitives;

namespace Customers.Domain.Customers;

public sealed class CustomerSession : Entity<Guid>
{
    private CustomerSession()
    {
    }

    public Guid? CustomerId { get; private set; }

    public string SessionId { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime LastSeenUtc { get; private set; }

    public static CustomerSession Create(string sessionId, Guid? customerId)
    {
        return new CustomerSession
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId.Trim(),
            CustomerId = customerId,
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow,
        };
    }

    public void Touch(Guid? customerId)
    {
        if (customerId is not null)
        {
            CustomerId = customerId;
        }

        LastSeenUtc = DateTime.UtcNow;
    }
}
