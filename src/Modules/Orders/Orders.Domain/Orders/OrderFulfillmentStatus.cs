namespace Orders.Domain.Orders;

public enum OrderFulfillmentStatus
{
    Unfulfilled = 1,
    FulfillmentPending = 2,
    Fulfilled = 3,
    Returned = 4,
}
