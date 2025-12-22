using FluentValidation;
using ProductIntelligence.Application.Commands.Features;

namespace ProductIntelligence.Application.Validators;

public class UpdateFeaturePriorityCommandValidator : AbstractValidator<UpdateFeaturePriorityCommand>
{
    public UpdateFeaturePriorityCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Feature ID is required");

        RuleFor(x => x.PriorityScore)
            .InclusiveBetween(0, 1).WithMessage("Priority score must be between 0 and 1");

        RuleFor(x => x.Reasoning)
            .NotEmpty().WithMessage("Reasoning is required")
            .MaximumLength(5000).WithMessage("Reasoning must not exceed 5000 characters");
    }
}
