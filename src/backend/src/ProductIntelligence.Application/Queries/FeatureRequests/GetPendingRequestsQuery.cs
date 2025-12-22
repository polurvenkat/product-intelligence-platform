using MediatR;
using ProductIntelligence.Application.Commands.FeatureRequests;

namespace ProductIntelligence.Application.Queries.FeatureRequests;

/// <summary>
/// Query to retrieve all pending feature requests
/// </summary>
public record GetPendingRequestsQuery : IRequest<IEnumerable<FeatureRequestDto>>
{
    /// <summary>
    /// Optional limit on number of results (default: 100)
    /// </summary>
    public int Limit { get; init; } = 100;
    
    /// <summary>
    /// Optional offset for pagination
    /// </summary>
    public int Offset { get; init; } = 0;
}
