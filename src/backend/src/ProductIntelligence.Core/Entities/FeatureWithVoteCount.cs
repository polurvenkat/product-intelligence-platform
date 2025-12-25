using System;

namespace ProductIntelligence.Core.Entities;

public record FeatureWithVoteCount
{
    public Guid Id { get; init; }
    public Guid DomainId { get; init; }
    public Guid? ParentFeatureId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public decimal AiPriorityScore { get; init; }
    public string? AiPriorityReasoning { get; init; }
    public int? EstimatedEffortPoints { get; init; }
    public decimal? BusinessValueScore { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? TargetRelease { get; init; }
    public string? Metadata { get; init; }
    public long VoteCount { get; init; }
    public long WeightedVoteCount { get; init; }
}
