using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Votes.Queries;

/// <summary>
/// Query to get vote count for a specific feature.
/// </summary>
public record GetVoteCountQuery(Guid FeatureId) : IRequest<VoteCountDto>;
