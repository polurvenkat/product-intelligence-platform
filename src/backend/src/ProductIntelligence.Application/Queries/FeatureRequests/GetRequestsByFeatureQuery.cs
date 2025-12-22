using MediatR;
using ProductIntelligence.Application.Commands.FeatureRequests;

namespace ProductIntelligence.Application.Queries.FeatureRequests;

/// <summary>
/// Query to get all feature requests linked to a specific feature.
/// </summary>
public record GetRequestsByFeatureQuery(Guid FeatureId) : IRequest<IEnumerable<FeatureRequestDto>>;
