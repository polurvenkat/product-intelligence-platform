using MediatR;
using Microsoft.Extensions.Logging;
using ProductIntelligence.Application.Commands.Intelligence;
using ProductIntelligence.Infrastructure.AI.Models;
using AIMatchType = ProductIntelligence.Infrastructure.AI.Models.MatchType;

namespace ProductIntelligence.Infrastructure.AI.Handlers;

/// <summary>
/// Handler for analyzing feature requests for duplicates using AI
/// Located in Infrastructure layer to access AI services without circular dependency
/// </summary>
public class AnalyzeFeatureRequestCommandHandler 
    : IRequestHandler<AnalyzeFeatureRequestCommand, DeduplicationResultDto>
{
    private readonly IFeatureDeduplicationAgent _deduplicationAgent;
    private readonly ILogger<AnalyzeFeatureRequestCommandHandler> _logger;

    public AnalyzeFeatureRequestCommandHandler(
        IFeatureDeduplicationAgent deduplicationAgent,
        ILogger<AnalyzeFeatureRequestCommandHandler> logger)
    {
        _deduplicationAgent = deduplicationAgent ?? throw new ArgumentNullException(nameof(deduplicationAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeduplicationResultDto> Handle(
        AnalyzeFeatureRequestCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing deduplication analysis for feature request: {Title}",
            command.Title);

        try
        {
            // Create deduplication request
            var request = new DeduplicationRequest
            {
                Title = command.Title,
                Description = command.Description,
                ExcludeRequestId = command.ExcludeRequestId,
                SimilarityThreshold = command.SimilarityThreshold,
                MaxResults = command.MaxResults
            };

            // Analyze using AI agent
            var result = await _deduplicationAgent.AnalyzeAsync(request, cancellationToken);

            _logger.LogInformation(
                "Deduplication analysis complete. Found {DuplicateCount} duplicates, {SimilarCount} similar requests in {Time}ms",
                result.Matches.Count(m => m.MatchType == AIMatchType.Duplicate),
                result.Matches.Count(m => m.MatchType == AIMatchType.Similar),
                result.ProcessingTimeMs);

            // Map to DTO
            return new DeduplicationResultDto
            {
                HasDuplicates = result.HasDuplicates,
                HasSimilar = result.HasSimilar,
                Matches = result.Matches.Select(MapToDto).ToList(),
                Summary = result.Summary,
                Reasoning = result.Reasoning,
                ProcessingTimeMs = result.ProcessingTimeMs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze feature request for duplicates: {Title}", command.Title);
            throw;
        }
    }

    private static DuplicateMatchDto MapToDto(DuplicateMatch match)
    {
        return new DuplicateMatchDto
        {
            RequestId = match.RequestId,
            Title = match.Title,
            Description = match.Description,
            SimilarityScore = match.SimilarityScore,
            ConfidenceScore = match.ConfidenceScore,
            MatchType = match.MatchType.ToString(),
            Reasoning = match.Reasoning,
            RequesterName = match.RequesterName,
            RequesterCompany = match.RequesterCompany,
            SubmittedAt = match.SubmittedAt,
            Status = match.Status
        };
    }
}
