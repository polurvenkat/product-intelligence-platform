using FluentValidation;

namespace ProductIntelligence.Application.Commands.Intelligence;

/// <summary>
/// Validator for AnalyzeFeatureRequestCommand
/// </summary>
public class AnalyzeFeatureRequestCommandValidator : AbstractValidator<AnalyzeFeatureRequestCommand>
{
    public AnalyzeFeatureRequestCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(500)
            .WithMessage("Title cannot exceed 500 characters")
            .MinimumLength(5)
            .WithMessage("Title must be at least 5 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MinimumLength(20)
            .WithMessage("Description must be at least 20 characters for accurate analysis")
            .MaximumLength(5000)
            .WithMessage("Description cannot exceed 5000 characters");

        RuleFor(x => x.SimilarityThreshold)
            .InclusiveBetween(0.5, 1.0)
            .WithMessage("Similarity threshold must be between 0.5 and 1.0");

        RuleFor(x => x.MaxResults)
            .InclusiveBetween(1, 50)
            .WithMessage("Max results must be between 1 and 50");
    }
}
