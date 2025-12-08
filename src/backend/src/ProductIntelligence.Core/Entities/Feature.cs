using Ardalis.GuardClauses;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.ValueObjects;

namespace ProductIntelligence.Core.Entities;

/// <summary>
/// Represents an accepted feature that is tracked in the product roadmap.
/// Features belong to a domain and can have parent-child relationships for sub-features.
/// </summary>
public class Feature
{
    public Guid Id { get; private set; }
    public Guid DomainId { get; private set; }
    public Guid? ParentFeatureId { get; private set; }
    
    public string Title { get; private set; }
    public string Description { get; private set; }
    
    public FeatureStatus Status { get; private set; }
    public Priority Priority { get; private set; }
    
    /// <summary>
    /// AI-calculated priority score (0.00 - 1.00)
    /// </summary>
    public decimal AiPriorityScore { get; private set; }
    
    /// <summary>
    /// AI-generated reasoning for the priority recommendation
    /// </summary>
    public string? AiPriorityReasoning { get; private set; }
    
    public int? EstimatedEffortPoints { get; private set; }
    public decimal? BusinessValueScore { get; private set; }
    
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    public string? TargetRelease { get; private set; }
    public Dictionary<string, object>? Metadata { get; private set; }

    // Navigation properties
    public Domain? Domain { get; set; }
    public List<FeatureRequest> LinkedRequests { get; set; } = new();
    public List<Feedback> Feedback { get; set; } = new();

    private Feature() { } // For Dapper

    public Feature(
        Guid domainId,
        string title,
        string description,
        Guid createdBy,
        Guid? parentFeatureId = null,
        Priority priority = Priority.P3,
        decimal aiPriorityScore = 0.5m,
        string? aiPriorityReasoning = null)
    {
        Id = Guid.NewGuid();
        DomainId = Guard.Against.Default(domainId, nameof(domainId));
        Title = Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        CreatedBy = Guard.Against.Default(createdBy, nameof(createdBy));
        ParentFeatureId = parentFeatureId;
        Status = FeatureStatus.Proposed;
        Priority = priority;
        AiPriorityScore = Guard.Against.OutOfRange(aiPriorityScore, nameof(aiPriorityScore), 0m, 1m);
        AiPriorityReasoning = aiPriorityReasoning;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(FeatureStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePriority(Priority priority, decimal? aiScore = null, string? aiReasoning = null)
    {
        Priority = priority;
        if (aiScore.HasValue)
        {
            AiPriorityScore = Guard.Against.OutOfRange(aiScore.Value, nameof(aiScore), 0m, 1m);
        }
        if (aiReasoning != null)
        {
            AiPriorityReasoning = aiReasoning;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string title, string description, int? effortPoints, decimal? businessValue)
    {
        Title = Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        EstimatedEffortPoints = effortPoints;
        BusinessValueScore = businessValue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTargetRelease(string release)
    {
        TargetRelease = Guard.Against.NullOrWhiteSpace(release, nameof(release));
        UpdatedAt = DateTime.UtcNow;
    }
}
