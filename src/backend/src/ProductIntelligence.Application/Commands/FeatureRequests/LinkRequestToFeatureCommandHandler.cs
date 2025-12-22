using MediatR;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Commands.FeatureRequests;

public class LinkRequestToFeatureCommandHandler : IRequestHandler<LinkRequestToFeatureCommand, bool>
{
    private readonly IFeatureRequestRepository _requestRepository;
    private readonly IFeatureRepository _featureRepository;

    public LinkRequestToFeatureCommandHandler(
        IFeatureRequestRepository requestRepository,
        IFeatureRepository featureRepository)
    {
        _requestRepository = requestRepository;
        _featureRepository = featureRepository;
    }

    public async Task<bool> Handle(LinkRequestToFeatureCommand request, CancellationToken cancellationToken)
    {
        // Validate request exists
        var featureRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
        if (featureRequest == null)
        {
            throw new KeyNotFoundException($"Feature request with ID {request.RequestId} not found");
        }

        // Validate feature exists
        var feature = await _featureRepository.GetByIdAsync(request.FeatureId, cancellationToken);
        if (feature == null)
        {
            throw new KeyNotFoundException($"Feature with ID {request.FeatureId} not found");
        }

        // Link the request to the feature
        featureRequest.LinkToFeature(request.FeatureId);

        // Update the request
        await _requestRepository.UpdateAsync(featureRequest, cancellationToken);

        return true;
    }
}
