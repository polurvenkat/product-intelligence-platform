using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.DTOs;

/// <summary>
/// Data transfer object for feature votes.
/// </summary>
public record FeatureVoteDto
{
    public Guid Id { get; init; }
    public Guid? FeatureId { get; init; }
    public Guid? FeatureRequestId { get; init; }
    public string VoterEmail { get; init; } = string.Empty;
    public string? VoterCompany { get; init; }
    public CustomerTier VoterTier { get; init; }
    public int VoteWeight { get; init; }
    public DateTime VotedAt { get; init; }
}

/// <summary>
/// Data transfer object for vote counts.
/// </summary>
public record VoteCountDto
{
    public long Count { get; init; }
    public long WeightedCount { get; init; }
}
