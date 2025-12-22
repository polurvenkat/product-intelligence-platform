using MediatR;
using ProductIntelligence.Application.Feedback.Commands;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Feedback.Handlers;

public class DeleteFeedbackCommandHandler : IRequestHandler<DeleteFeedbackCommand, bool>
{
    private readonly IFeedbackRepository _feedbackRepository;

    public DeleteFeedbackCommandHandler(IFeedbackRepository feedbackRepository)
    {
        _feedbackRepository = feedbackRepository;
    }

    public async Task<bool> Handle(DeleteFeedbackCommand request, CancellationToken cancellationToken)
    {
        // Verify feedback exists
        var feedback = await _feedbackRepository.GetByIdAsync(request.Id, cancellationToken);
        if (feedback == null)
        {
            throw new KeyNotFoundException($"Feedback with ID {request.Id} not found");
        }

        await _feedbackRepository.DeleteAsync(request.Id, cancellationToken);
        return true;
    }
}
