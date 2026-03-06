namespace Orders.Domain.Orders;

public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    PaymentFailed = 3,
    Cancelled = 4,
    Refunded = 5,
    PartiallyRefunded = 6,
}
