using Ardalis.GuardClauses;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Core.Entities;

/// <summary>
/// Represents a raw feature request from customers, users, or integrations.
/// Requests are processed by AI to detect duplicates and recommend priorities.
/// </summary>
public class FeatureRequest
{
    public Guid Id { get; private set; }
    
    public string Title { get; private set; }
    public string Description { get; private set; }
    
    public RequestSource Source { get; private set; }
    public string? SourceId { get; private set; }
    
    public string RequesterName { get; private set; }
    public string? RequesterEmail { get; private set; }
    public string? RequesterCompany { get; private set; }
    public CustomerTier RequesterTier { get; private set; }
    
    public DateTime SubmittedAt { get; private set; }
    public RequestStatus Status { get; private set; }
    
    // AI Processing
    public float[]? EmbeddingVector { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    
    // Linking
    public Guid? LinkedFeatureId { get; private set; }
    public Guid? DuplicateOfRequestId { get; private set; }
    public decimal? SimilarityScore { get; private set; }
    
    public Dictionary<string, object>? Metadata { get; private set; }

    // Navigation
    public Feature? LinkedFeature { get; set; }

    private FeatureRequest() { } // For Dapper

    public FeatureRequest(
        string title,
        string description,
        string requesterName,
        RequestSource source = RequestSource.Manual,
        string? sourceId = null,
        string? requesterEmail = null,
        string? requesterCompany = null,
        CustomerTier requesterTier = CustomerTier.Starter)
    {
        Id = Guid.NewGuid();
        Title = Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        RequesterName = Guard.Against.NullOrWhiteSpace(requesterName, nameof(requesterName));
        Source = source;
        SourceId = sourceId;
        RequesterEmail = requesterEmail;
        RequesterCompany = requesterCompany;
        RequesterTier = requesterTier;
        SubmittedAt = DateTime.UtcNow;
        Status = RequestStatus.Pending;
    }

    public void SetEmbedding(float[] embedding)
    {
        Guard.Against.NullOrEmpty(embedding, nameof(embedding));
        EmbeddingVector = embedding;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsDuplicate(Guid duplicateOfRequestId, decimal similarityScore)
    {
        DuplicateOfRequestId = duplicateOfRequestId;
        SimilarityScore = Guard.Against.OutOfRange(similarityScore, nameof(similarityScore), 0m, 1m);
        Status = RequestStatus.Duplicate;
    }

    public void LinkToFeature(Guid featureId)
    {
        LinkedFeatureId = featureId;
        Status = RequestStatus.Accepted;
    }

    public void Accept()
    {
        Status = RequestStatus.Accepted;
    }

    public void Reject()
    {
        Status = RequestStatus.Rejected;
    }

    public void SetUnderReview()
    {
        Status = RequestStatus.Reviewing;
    }
}
