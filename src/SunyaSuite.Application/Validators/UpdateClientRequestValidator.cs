using FluentValidation;
using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Validators;

public class UpdateClientRequestValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .MaximumLength(200)
            .EmailAddress().WithMessage("Enter a valid email address");

        RuleFor(x => x.Company)
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(50);

        RuleFor(x => x.Address)
            .MaximumLength(500);
    }
}
