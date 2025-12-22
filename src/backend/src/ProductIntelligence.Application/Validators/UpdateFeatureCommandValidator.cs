using FluentValidation;
using ProductIntelligence.Application.Commands.Features;

namespace ProductIntelligence.Application.Validators;

public class UpdateFeatureCommandValidator : AbstractValidator<UpdateFeatureCommand>
{
    public UpdateFeatureCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Feature ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MinimumLength(20).WithMessage("Description must be at least 20 characters")
            .MaximumLength(10000).WithMessage("Description must not exceed 10000 characters");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value");

        RuleFor(x => x.EstimatedEffortPoints)
            .GreaterThan(0).WithMessage("Effort points must be greater than 0")
            .When(x => x.EstimatedEffortPoints.HasValue);

        RuleFor(x => x.BusinessValueScore)
            .InclusiveBetween(0, 1).WithMessage("Business value score must be between 0 and 1")
            .When(x => x.BusinessValueScore.HasValue);
    }
}
