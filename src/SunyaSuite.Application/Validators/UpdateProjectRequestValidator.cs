using FluentValidation;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.Validators;

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.ClientId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Deadline)
            .NotEmpty();

        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<ProjectStatus>(s, out _))
            .WithMessage("Invalid project status.");

        RuleFor(x => x.ProgressPercent)
            .InclusiveBetween(0, 100);
    }
}
