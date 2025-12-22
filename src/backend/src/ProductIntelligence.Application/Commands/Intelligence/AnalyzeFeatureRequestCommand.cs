using MediatR;

namespace ProductIntelligence.Application.Commands.Intelligence;

/// <summary>
/// Command to analyze a feature request for duplicates and similar requests
/// </summary>
public record AnalyzeFeatureRequestCommand : IRequest<DeduplicationResultDto>
{
    /// <summary>
    /// Feature request title
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Feature request description
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Optional request ID to exclude from results (when analyzing an existing request)
    /// </summary>
    public Guid? ExcludeRequestId { get; init; }

    /// <summary>
    /// Minimum similarity threshold (0.0-1.0). Defaults to 0.70
    /// </summary>
    public double SimilarityThreshold { get; init; } = 0.70;

    /// <summary>
    /// Maximum number of similar requests to analyze. Defaults to 10
    /// </summary>
    public int MaxResults { get; init; } = 10;
}

/// <summary>
/// Result DTO for deduplication analysis
/// </summary>
public record DeduplicationResultDto
{
    public required bool HasDuplicates { get; init; }
    public required bool HasSimilar { get; init; }
    public required List<DuplicateMatchDto> Matches { get; init; }
    public required string Summary { get; init; }
    public required string Reasoning { get; init; }
    public required long ProcessingTimeMs { get; init; }
}

/// <summary>
/// DTO for a single duplicate match
/// </summary>
public record DuplicateMatchDto
{
    public required Guid RequestId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required double SimilarityScore { get; init; }
    public required double ConfidenceScore { get; init; }
    public required string MatchType { get; init; }
    public required string Reasoning { get; init; }
    public string? RequesterName { get; init; }
    public string? RequesterCompany { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required string Status { get; init; }
}
