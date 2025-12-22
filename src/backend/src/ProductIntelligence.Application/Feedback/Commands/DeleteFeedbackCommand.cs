using MediatR;

namespace ProductIntelligence.Application.Feedback.Commands;

/// <summary>
/// Command to delete feedback.
/// </summary>
public record DeleteFeedbackCommand(Guid Id) : IRequest<bool>;
