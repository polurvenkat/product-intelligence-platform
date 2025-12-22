using FluentValidation;

namespace ProductIntelligence.Application.Feedback.Commands;

public class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Feedback content is required")
            .MinimumLength(10)
            .WithMessage("Feedback content must be at least 10 characters")
            .MaximumLength(5000)
            .WithMessage("Feedback content cannot exceed 5000 characters");

        RuleFor(x => x)
            .Must(x => x.FeatureId.HasValue || x.FeatureRequestId.HasValue)
            .WithMessage("Feedback must be linked to either a Feature or FeatureRequest");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Valid source is required");

        RuleFor(x => x.CustomerTier)
            .IsInEnum()
            .When(x => x.CustomerTier.HasValue)
            .WithMessage("Valid customer tier is required when specified");
    }
}
