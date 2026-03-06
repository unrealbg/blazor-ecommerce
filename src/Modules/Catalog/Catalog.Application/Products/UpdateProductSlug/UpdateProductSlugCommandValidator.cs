using FluentValidation;

namespace Catalog.Application.Products.UpdateProductSlug;

public sealed class UpdateProductSlugCommandValidator : AbstractValidator<UpdateProductSlugCommand>
{
    public UpdateProductSlugCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty();

        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(220);
    }
}
