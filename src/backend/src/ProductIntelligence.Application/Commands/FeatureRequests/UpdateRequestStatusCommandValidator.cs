using FluentValidation;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Commands.FeatureRequests;

public class UpdateRequestStatusCommandValidator : AbstractValidator<UpdateRequestStatusCommand>
{
    public UpdateRequestStatusCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("Request ID is required");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Valid status is required")
            .NotEqual(RequestStatus.Pending)
            .WithMessage("Cannot manually set status to Pending")
            .NotEqual(RequestStatus.Duplicate)
            .WithMessage("Use MarkRequestAsDuplicate command to set status to Duplicate");
    }
}
