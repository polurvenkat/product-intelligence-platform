using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Feedback.Queries;

public class GetFeedbackByFeatureQueryHandler : IRequestHandler<GetFeedbackByFeatureQuery, IEnumerable<FeedbackDto>>
{
    private readonly IFeedbackRepository _feedbackRepository;

    public GetFeedbackByFeatureQueryHandler(IFeedbackRepository feedbackRepository)
    {
        _feedbackRepository = feedbackRepository;
    }

    public async Task<IEnumerable<FeedbackDto>> Handle(GetFeedbackByFeatureQuery request, CancellationToken cancellationToken)
    {
        var feedbacks = await _feedbackRepository.GetByFeatureIdAsync(request.FeatureId, cancellationToken);

        return feedbacks.Select(f => new FeedbackDto
        {
            Id = f.Id,
            FeatureId = f.FeatureId,
            FeatureRequestId = f.FeatureRequestId,
            Content = f.Content,
            Sentiment = f.Sentiment,
            Source = f.Source,
            CustomerId = f.CustomerId,
            CustomerTier = f.CustomerTier,
            SubmittedAt = f.SubmittedAt
        });
    }
}
