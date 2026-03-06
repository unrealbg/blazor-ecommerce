using FluentValidation;

namespace Inventory.Application.Stock.AdjustStock;

public sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.QuantityDelta)
            .NotEqual(0);

        RuleFor(command => command.Reason)
            .MaximumLength(300);

        RuleFor(command => command.CreatedBy)
            .MaximumLength(120);
    }
}
