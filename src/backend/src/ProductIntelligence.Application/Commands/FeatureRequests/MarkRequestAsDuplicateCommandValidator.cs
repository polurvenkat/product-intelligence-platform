using FluentValidation;

namespace ProductIntelligence.Application.Commands.FeatureRequests;

public class MarkRequestAsDuplicateCommandValidator : AbstractValidator<MarkRequestAsDuplicateCommand>
{
    public MarkRequestAsDuplicateCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("Request ID is required");

        RuleFor(x => x.DuplicateOfRequestId)
            .NotEmpty()
            .WithMessage("Duplicate of request ID is required");

        RuleFor(x => x.SimilarityScore)
            .InclusiveBetween(0m, 1m)
            .WithMessage("Similarity score must be between 0 and 1");

        RuleFor(x => x)
            .Must(x => x.RequestId != x.DuplicateOfRequestId)
            .WithMessage("Request cannot be marked as duplicate of itself");
    }
}
