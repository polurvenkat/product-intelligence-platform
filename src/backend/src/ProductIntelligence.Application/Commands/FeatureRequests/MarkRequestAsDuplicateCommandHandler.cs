using MediatR;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Commands.FeatureRequests;

public class MarkRequestAsDuplicateCommandHandler : IRequestHandler<MarkRequestAsDuplicateCommand, bool>
{
    private readonly IFeatureRequestRepository _requestRepository;

    public MarkRequestAsDuplicateCommandHandler(IFeatureRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<bool> Handle(MarkRequestAsDuplicateCommand request, CancellationToken cancellationToken)
    {
        // Validate the request to be marked as duplicate exists
        var featureRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
        if (featureRequest == null)
        {
            throw new KeyNotFoundException($"Feature request with ID {request.RequestId} not found");
        }

        // Validate the original request exists
        var originalRequest = await _requestRepository.GetByIdAsync(request.DuplicateOfRequestId, cancellationToken);
        if (originalRequest == null)
        {
            throw new KeyNotFoundException($"Original feature request with ID {request.DuplicateOfRequestId} not found");
        }

        // Prevent marking a request as duplicate of itself
        if (request.RequestId == request.DuplicateOfRequestId)
        {
            throw new InvalidOperationException("Cannot mark a request as duplicate of itself");
        }

        // Mark as duplicate
        featureRequest.MarkAsDuplicate(request.DuplicateOfRequestId, request.SimilarityScore);

        // Update the request
        await _requestRepository.UpdateAsync(featureRequest, cancellationToken);

        return true;
    }
}
