using MediatR;
using ProductIntelligence.Application.Commands.FeatureRequests;

namespace ProductIntelligence.Application.Queries.FeatureRequests;

/// <summary>
/// Query to get all feature requests marked as duplicates of a specific request.
/// </summary>
public record GetDuplicateRequestsQuery(Guid OriginalRequestId) : IRequest<IEnumerable<FeatureRequestDto>>;
