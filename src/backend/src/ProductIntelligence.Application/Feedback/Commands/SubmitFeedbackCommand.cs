using MediatR;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Feedback.Commands;

/// <summary>
/// Command to submit new feedback for a feature or feature request.
/// </summary>
public record SubmitFeedbackCommand : IRequest<Guid>
{
    public Guid? FeatureId { get; init; }
    public Guid? FeatureRequestId { get; init; }
    public string Content { get; init; } = string.Empty;
    public RequestSource Source { get; init; }
    public string? CustomerId { get; init; }
    public CustomerTier? CustomerTier { get; init; }
}
