using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Feedback.Queries;

public record GetAllFeedbackQuery : IRequest<IEnumerable<FeedbackDto>>
{
    public int Limit { get; init; } = 100;
    public int Offset { get; init; } = 0;
}
