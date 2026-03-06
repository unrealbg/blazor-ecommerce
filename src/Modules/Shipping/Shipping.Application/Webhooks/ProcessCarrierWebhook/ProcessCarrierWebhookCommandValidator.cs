using FluentValidation;

namespace Shipping.Application.Webhooks.ProcessCarrierWebhook;

public sealed class ProcessCarrierWebhookCommandValidator : AbstractValidator<ProcessCarrierWebhookCommand>
{
    public ProcessCarrierWebhookCommandValidator()
    {
        RuleFor(command => command.Provider).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Payload).NotEmpty();
    }
}
