using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Feedback.Queries;

/// <summary>
/// Query to get all feedback for a specific feature.
/// </summary>
public record GetFeedbackByFeatureQuery(Guid FeatureId) : IRequest<IEnumerable<FeedbackDto>>;
