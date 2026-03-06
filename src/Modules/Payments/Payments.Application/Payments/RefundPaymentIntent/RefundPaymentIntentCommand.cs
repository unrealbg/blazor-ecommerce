using BuildingBlocks.Application.Abstractions;

namespace Payments.Application.Payments.RefundPaymentIntent;

public sealed record RefundPaymentIntentCommand(
    Guid PaymentIntentId,
    decimal? Amount,
    string? Reason) : ICommand<PaymentIntentActionResult>;
