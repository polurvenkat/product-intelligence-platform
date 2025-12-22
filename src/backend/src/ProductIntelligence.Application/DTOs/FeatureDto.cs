using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.DTOs;

/// <summary>
/// Data transfer object for feature information
/// </summary>
public record FeatureDto
{
    public Guid Id { get; init; }
    public Guid DomainId { get; init; }
    public string DomainName { get; init; } = string.Empty;
    public Guid? ParentFeatureId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public FeatureStatus Status { get; init; }
    public Priority Priority { get; init; }
    public decimal AiPriorityScore { get; init; }
    public string? AiPriorityReasoning { get; init; }
    public int? EstimatedEffortPoints { get; init; }
    public decimal? BusinessValueScore { get; init; }
    public Guid CreatedBy { get; init; }
    public string? TargetRelease { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
