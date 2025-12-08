using Ardalis.GuardClauses;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Core.Entities;

/// <summary>
/// Tracks votes/support for features or feature requests.
/// Enterprise customers have higher vote weight for prioritization.
/// </summary>
public class FeatureVote
{
    public Guid Id { get; private set; }
    public Guid? FeatureId { get; private set; }
    public Guid? FeatureRequestId { get; private set; }
    
    public string VoterEmail { get; private set; }
    public string? VoterCompany { get; private set; }
    public CustomerTier VoterTier { get; private set; }
    
    /// <summary>
    /// Weight multiplier based on customer tier (Enterprise = 3x, Professional = 2x, Starter = 1x)
    /// </summary>
    public int VoteWeight { get; private set; }
    
    public DateTime VotedAt { get; private set; }

    private FeatureVote() { } // For Dapper

    public FeatureVote(
        string voterEmail,
        CustomerTier voterTier,
        Guid? featureId = null,
        Guid? featureRequestId = null,
        string? voterCompany = null)
    {
        Id = Guid.NewGuid();
        VoterEmail = Guard.Against.NullOrWhiteSpace(voterEmail, nameof(voterEmail));
        VoterTier = voterTier;
        VoterCompany = voterCompany;
        FeatureId = featureId;
        FeatureRequestId = featureRequestId;
        VotedAt = DateTime.UtcNow;

        if (featureId == null && featureRequestId == null)
        {
            throw new ArgumentException("Vote must be for either a Feature or FeatureRequest");
        }

        // Calculate weight based on tier
        VoteWeight = voterTier switch
        {
            CustomerTier.Enterprise => 3,
            CustomerTier.Professional => 2,
            CustomerTier.Starter => 1,
            _ => 1
        };
    }
}
