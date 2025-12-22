namespace ProductIntelligence.Infrastructure.AI.Models;

/// <summary>
/// Request to analyze a feature request for duplicates
/// </summary>
public record DeduplicationRequest
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
/// Result of deduplication analysis
/// </summary>
public record DeduplicationResult
{
    /// <summary>
    /// Whether any duplicates were found (confidence >= 90%)
    /// </summary>
    public required bool HasDuplicates { get; init; }

    /// <summary>
    /// Whether any similar requests were found (confidence >= 70%)
    /// </summary>
    public required bool HasSimilar { get; init; }

    /// <summary>
    /// List of matching requests with analysis
    /// </summary>
    public required IReadOnlyList<DuplicateMatch> Matches { get; init; }

    /// <summary>
    /// High-level summary of findings
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Overall reasoning from AI analysis
    /// </summary>
    public required string Reasoning { get; init; }

    /// <summary>
    /// The embedding vector generated for the request
    /// </summary>
    public required float[] EmbeddingVector { get; init; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public required long ProcessingTimeMs { get; init; }
}

/// <summary>
/// A single matching feature request with similarity and AI analysis
/// </summary>
public record DuplicateMatch
{
    /// <summary>
    /// ID of the matching feature request
    /// </summary>
    public required Guid RequestId { get; init; }

    /// <summary>
    /// Title of the matching request
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Description of the matching request
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Vector cosine similarity score (0.0-1.0)
    /// </summary>
    public required double SimilarityScore { get; init; }

    /// <summary>
    /// AI confidence score for this match (0.0-1.0)
    /// </summary>
    public required double ConfidenceScore { get; init; }

    /// <summary>
    /// Classification of the match type
    /// </summary>
    public required MatchType MatchType { get; init; }

    /// <summary>
    /// AI-generated reasoning for the classification
    /// </summary>
    public required string Reasoning { get; init; }

    /// <summary>
    /// Requester information for context
    /// </summary>
    public string? RequesterName { get; init; }

    /// <summary>
    /// Company information for context
    /// </summary>
    public string? RequesterCompany { get; init; }

    /// <summary>
    /// When this request was submitted
    /// </summary>
    public required DateTime SubmittedAt { get; init; }

    /// <summary>
    /// Current status of the request
    /// </summary>
    public required string Status { get; init; }
}

/// <summary>
/// Classification of how a request matches another
/// </summary>
public enum MatchType
{
    /// <summary>
    /// Exact duplicate - same request, different wording (>90% confidence)
    /// </summary>
    Duplicate,

    /// <summary>
    /// Very similar intent with minor differences (70-90% confidence)
    /// </summary>
    Similar,

    /// <summary>
    /// Related but different feature (50-70% confidence)
    /// </summary>
    Related
}

/// <summary>
/// Internal model for GPT-4o structured response
/// </summary>
internal record AiDeduplicationResponse
{
    public required List<AiMatchAnalysis> Matches { get; init; }
    public required string Summary { get; init; }
    public required string OverallReasoning { get; init; }
}

/// <summary>
/// Internal model for individual match analysis from GPT-4o
/// </summary>
internal record AiMatchAnalysis
{
    public required Guid RequestId { get; init; }
    public required string MatchType { get; init; }  // "Duplicate", "Similar", "Related"
    public required double Confidence { get; init; }
    public required string Reasoning { get; init; }
}
