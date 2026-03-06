using BuildingBlocks.Application.Abstractions;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.ListPaymentIntents;

public sealed record ListPaymentIntentsQuery(
    string? Provider,
    PaymentIntentStatus? Status,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    int Page,
    int PageSize) : IQuery<PaymentIntentPage>;
