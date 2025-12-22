using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Feedback.Queries;

public class GetFeedbackQueryHandler : IRequestHandler<GetFeedbackQuery, FeedbackDto?>
{
    private readonly IFeedbackRepository _feedbackRepository;

    public GetFeedbackQueryHandler(IFeedbackRepository feedbackRepository)
    {
        _feedbackRepository = feedbackRepository;
    }

    public async Task<FeedbackDto?> Handle(GetFeedbackQuery request, CancellationToken cancellationToken)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(request.Id, cancellationToken);

        if (feedback == null)
            return null;

        return new FeedbackDto
        {
            Id = feedback.Id,
            FeatureId = feedback.FeatureId,
            FeatureRequestId = feedback.FeatureRequestId,
            Content = feedback.Content,
            Sentiment = feedback.Sentiment,
            Source = feedback.Source,
            CustomerId = feedback.CustomerId,
            CustomerTier = feedback.CustomerTier,
            SubmittedAt = feedback.SubmittedAt
        };
    }
}
