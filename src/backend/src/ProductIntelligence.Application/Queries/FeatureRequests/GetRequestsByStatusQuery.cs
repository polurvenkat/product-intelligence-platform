using MediatR;
using ProductIntelligence.Application.Commands.FeatureRequests;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Queries.FeatureRequests;

/// <summary>
/// Query to get all feature requests with a specific status.
/// </summary>
public record GetRequestsByStatusQuery(RequestStatus Status) : IRequest<IEnumerable<FeatureRequestDto>>;
