using FluentValidation;

namespace ProductIntelligence.Application.Votes.Commands;

public class VoteForFeatureCommandValidator : AbstractValidator<VoteForFeatureCommand>
{
    public VoteForFeatureCommandValidator()
    {
        RuleFor(x => x.VoterEmail)
            .NotEmpty()
            .WithMessage("Voter email is required")
            .EmailAddress()
            .WithMessage("Valid email address is required");

        RuleFor(x => x)
            .Must(x => x.FeatureId.HasValue || x.FeatureRequestId.HasValue)
            .WithMessage("Vote must be for either a Feature or FeatureRequest");

        RuleFor(x => x.VoterTier)
            .IsInEnum()
            .WithMessage("Valid customer tier is required");
    }
}
