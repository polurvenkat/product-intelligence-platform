using MediatR;

namespace ProductIntelligence.Application.Votes.Commands;

/// <summary>
/// Command to remove a vote for a feature.
/// </summary>
public record RemoveVoteCommand(Guid FeatureId, string VoterEmail) : IRequest<bool>;
