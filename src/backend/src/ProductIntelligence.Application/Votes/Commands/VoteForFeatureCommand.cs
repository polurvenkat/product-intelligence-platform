using MediatR;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Votes.Commands;

/// <summary>
/// Command to vote for a feature or feature request.
/// </summary>
public record VoteForFeatureCommand : IRequest<Guid>
{
    public Guid? FeatureId { get; init; }
    public Guid? FeatureRequestId { get; init; }
    public string VoterEmail { get; init; } = string.Empty;
    public string? VoterCompany { get; init; }
    public CustomerTier VoterTier { get; init; }
}
