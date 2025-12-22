using FluentValidation;

namespace ProductIntelligence.Application.Commands.FeatureRequests;

public class LinkRequestToFeatureCommandValidator : AbstractValidator<LinkRequestToFeatureCommand>
{
    public LinkRequestToFeatureCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("Request ID is required");

        RuleFor(x => x.FeatureId)
            .NotEmpty()
            .WithMessage("Feature ID is required");
    }
}
