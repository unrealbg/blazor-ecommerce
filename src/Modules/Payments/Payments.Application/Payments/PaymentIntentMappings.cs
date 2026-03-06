using Payments.Domain.Payments;

namespace Payments.Application.Payments;

internal static class PaymentIntentMappings
{
    public static PaymentIntentActionResult ToActionResult(
        PaymentIntent paymentIntent,
        bool requiresAction,
        string? redirectUrl)
    {
        return new PaymentIntentActionResult(
            paymentIntent.Id,
            paymentIntent.Provider,
            paymentIntent.Status.ToString(),
            paymentIntent.ClientSecret,
            requiresAction,
            redirectUrl);
    }

    public static PaymentIntentSummaryDto ToSummaryDto(PaymentIntent paymentIntent)
    {
        return new PaymentIntentSummaryDto(
            paymentIntent.Id,
            paymentIntent.OrderId,
            paymentIntent.Provider,
            paymentIntent.Status.ToString(),
            paymentIntent.Amount,
            paymentIntent.Currency,
            paymentIntent.ProviderPaymentIntentId,
            paymentIntent.CreatedAtUtc,
            paymentIntent.UpdatedAtUtc,
            paymentIntent.CompletedAtUtc);
    }

    public static PaymentTransactionDto ToDto(PaymentTransaction transaction)
    {
        return new PaymentTransactionDto(
            transaction.Id,
            transaction.Type.ToString(),
            transaction.ProviderTransactionId,
            transaction.Amount,
            transaction.Currency,
            transaction.Status,
            transaction.RawReference,
            transaction.CreatedAtUtc,
            transaction.MetadataJson);
    }
}
