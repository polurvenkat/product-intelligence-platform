using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Feedback.Queries;

/// <summary>
/// Query to get feedback by ID.
/// </summary>
public record GetFeedbackQuery(Guid Id) : IRequest<FeedbackDto?>;
