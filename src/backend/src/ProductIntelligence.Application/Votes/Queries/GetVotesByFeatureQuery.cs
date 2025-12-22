using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Votes.Queries;

/// <summary>
/// Query to get all votes for a specific feature.
/// </summary>
public record GetVotesByFeatureQuery(Guid FeatureId) : IRequest<IEnumerable<FeatureVoteDto>>;
