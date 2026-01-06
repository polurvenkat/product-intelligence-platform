using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Feedback.Queries;

public class GetAllFeedbackQueryHandler : IRequestHandler<GetAllFeedbackQuery, IEnumerable<FeedbackDto>>
{
    private readonly IFeedbackRepository _feedbackRepository;

    public GetAllFeedbackQueryHandler(IFeedbackRepository feedbackRepository)
    {
        _feedbackRepository = feedbackRepository;
    }

    public async Task<IEnumerable<FeedbackDto>> Handle(GetAllFeedbackQuery request, CancellationToken cancellationToken)
    {
        var feedback = await _feedbackRepository.GetAllAsync(request.Limit, request.Offset, cancellationToken);
        
        return feedback.Select(f => new FeedbackDto
        {
            Id = f.Id,
            FeatureId = f.FeatureId,
            FeatureRequestId = f.FeatureRequestId,
            Content = f.Content,
            Sentiment = f.Sentiment,
            SentimentConfidence = f.SentimentConfidence,
            Source = f.Source,
            CustomerId = f.CustomerId,
            CustomerTier = f.CustomerTier,
            SubmittedAt = f.SubmittedAt
        });
    }
}
