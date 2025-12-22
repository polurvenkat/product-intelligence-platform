using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Feedback.Queries;

/// <summary>
/// Query to get all feedback for a specific feature request.
/// </summary>
public record GetFeedbackByRequestQuery(Guid FeatureRequestId) : IRequest<IEnumerable<FeedbackDto>>;
