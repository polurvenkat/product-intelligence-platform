using MediatR;

namespace ProductIntelligence.Application.Commands.FeatureRequests;

/// <summary>
/// Command to link a feature request to an existing feature.
/// </summary>
public record LinkRequestToFeatureCommand : IRequest<bool>
{
    public Guid RequestId { get; init; }
    public Guid FeatureId { get; init; }
}
