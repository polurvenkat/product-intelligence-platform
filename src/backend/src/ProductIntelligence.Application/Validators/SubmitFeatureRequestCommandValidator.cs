using FluentValidation;
using ProductIntelligence.Application.Commands.FeatureRequests;

namespace ProductIntelligence.Application.Validators;

/// <summary>
/// Validator for SubmitFeatureRequestCommand
/// </summary>
public class SubmitFeatureRequestCommandValidator : AbstractValidator<SubmitFeatureRequestCommand>
{
    public SubmitFeatureRequestCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MinimumLength(20).WithMessage("Description must be at least 20 characters")
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters");

        RuleFor(x => x.RequesterName)
            .NotEmpty().WithMessage("Requester name is required")
            .MaximumLength(100).WithMessage("Requester name must not exceed 100 characters");

        RuleFor(x => x.RequesterEmail)
            .EmailAddress().WithMessage("Invalid email address format")
            .When(x => !string.IsNullOrWhiteSpace(x.RequesterEmail));

        RuleFor(x => x.RequesterCompany)
            .MaximumLength(100).WithMessage("Company name must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.RequesterCompany));

        RuleFor(x => x.RequesterTier)
            .IsInEnum().WithMessage("Invalid customer tier");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Invalid request source");
    }
}
