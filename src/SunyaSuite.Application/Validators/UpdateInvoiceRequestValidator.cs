using FluentValidation;
using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Validators;

public sealed class UpdateInvoiceRequestValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.DueDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Due date must be today or later.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one line item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description)
                .NotEmpty()
                .MaximumLength(500);

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0);

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0);

            item.RuleFor(i => i.HsCode)
                .MinimumLength(4)
                .When(i => !string.IsNullOrEmpty(i.HsCode))
                .WithMessage("HS Code must be at least 4 digits.");
        });
    }
}
