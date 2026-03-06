namespace Payments.Domain.Payments;

public enum PaymentTransactionType
{
    Authorization = 1,
    Capture = 2,
    Charge = 3,
    Failure = 4,
    Refund = 5,
    WebhookEvent = 6,
    Cancellation = 7,
}
