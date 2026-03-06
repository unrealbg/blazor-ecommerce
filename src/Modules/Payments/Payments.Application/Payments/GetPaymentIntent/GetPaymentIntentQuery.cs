using BuildingBlocks.Application.Abstractions;

namespace Payments.Application.Payments.GetPaymentIntent;

public sealed record GetPaymentIntentQuery(Guid PaymentIntentId) : IQuery<PaymentIntentDetailsDto?>;
