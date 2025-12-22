using MediatR;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Commands.FeatureRequests;

/// <summary>
/// Command to update the status of a feature request.
/// </summary>
public record UpdateRequestStatusCommand : IRequest<bool>
{
    public Guid RequestId { get; init; }
    public RequestStatus Status { get; init; }
}
