namespace Payments.Domain.Payments;

public enum PaymentIntentStatus
{
    Created = 1,
    Pending = 2,
    RequiresAction = 3,
    Authorized = 4,
    Captured = 5,
    Failed = 6,
    Cancelled = 7,
    Refunded = 8,
    PartiallyRefunded = 9,
}
