using MediatR;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Commands.FeatureRequests;

public class UpdateRequestStatusCommandHandler : IRequestHandler<UpdateRequestStatusCommand, bool>
{
    private readonly IFeatureRequestRepository _requestRepository;

    public UpdateRequestStatusCommandHandler(IFeatureRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<bool> Handle(UpdateRequestStatusCommand request, CancellationToken cancellationToken)
    {
        // Validate request exists
        var featureRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
        if (featureRequest == null)
        {
            throw new KeyNotFoundException($"Feature request with ID {request.RequestId} not found");
        }

        // Update status based on the requested status
        switch (request.Status)
        {
            case RequestStatus.Reviewing:
                featureRequest.SetUnderReview();
                break;
            case RequestStatus.Accepted:
                featureRequest.Accept();
                break;
            case RequestStatus.Rejected:
                featureRequest.Reject();
                break;
            case RequestStatus.Pending:
            case RequestStatus.Duplicate:
                throw new InvalidOperationException(
                    $"Cannot manually set status to {request.Status}. " +
                    "Use appropriate commands (LinkToFeature or MarkAsDuplicate)");
            default:
                throw new ArgumentException($"Invalid status: {request.Status}");
        }

        // Update the request
        await _requestRepository.UpdateAsync(featureRequest, cancellationToken);

        return true;
    }
}
