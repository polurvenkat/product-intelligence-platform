using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.DTOs;

/// <summary>
/// Data transfer object for feedback.
/// </summary>
public record FeedbackDto
{
    public Guid Id { get; init; }
    public Guid? FeatureId { get; init; }
    public Guid? FeatureRequestId { get; init; }
    public string Content { get; init; } = string.Empty;
    public Sentiment Sentiment { get; init; }
    public RequestSource Source { get; init; }
    public string? CustomerId { get; init; }
    public CustomerTier? CustomerTier { get; init; }
    public DateTime SubmittedAt { get; init; }
}
