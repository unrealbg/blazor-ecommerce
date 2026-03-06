using FluentValidation;

namespace Payments.Application.Webhooks.ProcessWebhook;

public sealed class ProcessWebhookCommandValidator : AbstractValidator<ProcessWebhookCommand>
{
    public ProcessWebhookCommandValidator()
    {
        RuleFor(command => command.Provider).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Payload).NotEmpty();
    }
}
