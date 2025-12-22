using FluentValidation;
using ProductIntelligence.Application.Commands.Domains;

namespace ProductIntelligence.Application.Validators;

public class UpdateDomainCommandValidator : AbstractValidator<UpdateDomainCommand>
{
    public UpdateDomainCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Domain ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
