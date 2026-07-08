using FluentValidation;
using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Validators;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Deadline)
            .NotEmpty()
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Deadline must be today or later.");
    }
}
