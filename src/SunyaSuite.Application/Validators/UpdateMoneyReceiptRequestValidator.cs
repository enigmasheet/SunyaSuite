using FluentValidation;
using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Validators;

public sealed class UpdateMoneyReceiptRequestValidator : AbstractValidator<UpdateMoneyReceiptRequest>
{
    public UpdateMoneyReceiptRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.InvoiceIds)
            .NotEmpty().WithMessage("At least one invoice must be selected.");

        RuleFor(x => x.AllocatedAmounts)
            .NotEmpty().WithMessage("Allocation amounts are required.")
            .Must((request, amounts) => amounts.Length == request.InvoiceIds.Length)
            .WithMessage("Each invoice must have a corresponding allocated amount.")
            .ForEach(amount => amount.GreaterThan(0).WithMessage("Each allocated amount must be greater than zero."));

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Invalid payment method.");

        RuleFor(x => x.ReceivedFromName)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.ReceivedFromPan)
            .MaximumLength(15);

        RuleFor(x => x.ReceivedFromAddress)
            .MaximumLength(500);
    }
}
