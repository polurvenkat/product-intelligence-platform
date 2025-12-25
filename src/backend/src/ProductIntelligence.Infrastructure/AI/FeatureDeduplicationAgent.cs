using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Application.Interfaces.AI;
using ProductIntelligence.Infrastructure.AI.Models;
using AIMatchType = ProductIntelligence.Infrastructure.AI.Models.MatchType;

namespace ProductIntelligence.Infrastructure.AI;

/// <summary>
/// AI-powered agent for detecting duplicate and similar feature requests.
/// Uses vector similarity (pgvector) + GPT-4o analysis for high-accuracy deduplication.
/// </summary>
public class FeatureDeduplicationAgent : IFeatureDeduplicationAgent
{
    private readonly IAzureOpenAIService _openAIService;
    private readonly IFeatureRequestRepository _featureRequestRepository;
    private readonly ILogger<FeatureDeduplicationAgent> _logger;

    private const double DuplicateThreshold = 0.90;
    private const double SimilarThreshold = 0.70;
    private const double RelatedThreshold = 0.50;

    public FeatureDeduplicationAgent(
        IAzureOpenAIService openAIService,
        IFeatureRequestRepository featureRequestRepository,
        ILogger<FeatureDeduplicationAgent> logger)
    {
        _openAIService = openAIService ?? throw new ArgumentNullException(nameof(openAIService));
        _featureRequestRepository = featureRequestRepository ?? throw new ArgumentNullException(nameof(featureRequestRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeduplicationResult> AnalyzeAsync(
        DeduplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title cannot be empty", nameof(request));
        
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ArgumentException("Description cannot be empty", nameof(request));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting deduplication analysis for request: {Title}",
                request.Title);

            // Step 1: Generate embedding vector
            var embedding = await GenerateEmbeddingAsync(
                request.Title,
                request.Description,
                cancellationToken);

            _logger.LogDebug("Generated embedding vector with {Dimensions} dimensions", embedding.Length);

            // Step 2: Find similar requests using vector search
            var similarRequests = await FindSimilarRequestsAsync(
                embedding,
                request.SimilarityThreshold,
                request.MaxResults,
                request.ExcludeRequestId,
                cancellationToken);

            if (!similarRequests.Any())
            {
                _logger.LogInformation("No similar requests found above threshold {Threshold:P0}", 
                    request.SimilarityThreshold);

                stopwatch.Stop();
                return new DeduplicationResult
                {
                    HasDuplicates = false,
                    HasSimilar = false,
                    Matches = Array.Empty<DuplicateMatch>(),
                    Summary = "No similar requests found.",
                    Reasoning = "This appears to be a unique feature request with no existing duplicates.",
                    EmbeddingVector = embedding,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            _logger.LogInformation("Found {Count} similar requests, analyzing with GPT-4o", 
                similarRequests.Count);

            // Step 3: Use GPT-4o to analyze matches
            var aiAnalysis = await AnalyzeMatchesWithAIAsync(
                request,
                similarRequests,
                cancellationToken);

            // Step 4: Build result
            var matches = BuildMatchResults(similarRequests, aiAnalysis);

            var hasDuplicates = matches.Any(m => m.MatchType == AIMatchType.Duplicate);
            var hasSimilar = matches.Any(m => m.MatchType == AIMatchType.Similar);

            stopwatch.Stop();

            _logger.LogInformation(
                "Deduplication analysis complete. Duplicates: {Duplicates}, Similar: {Similar}, Time: {Time}ms",
                matches.Count(m => m.MatchType == AIMatchType.Duplicate),
                matches.Count(m => m.MatchType == AIMatchType.Similar),
                stopwatch.ElapsedMilliseconds);

            return new DeduplicationResult
            {
                HasDuplicates = hasDuplicates,
                HasSimilar = hasSimilar,
                Matches = matches.OrderByDescending(m => m.ConfidenceScore).ToList(),
                Summary = aiAnalysis.Summary,
                Reasoning = aiAnalysis.OverallReasoning,
                EmbeddingVector = embedding,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deduplication analysis for request: {Title}", request.Title);
            throw;
        }
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        // Combine title and description with weight towards title (appears twice)
        var text = $"{title}\n\n{title}\n\n{description}";

        try
        {
            var embedding = await _openAIService.GenerateEmbeddingAsync(text, cancellationToken);
            
            _logger.LogDebug("Generated embedding for text: {Preview}...", 
                text.Length > 100 ? text[..100] : text);

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for title: {Title}", title);
            throw;
        }
    }

    private async Task<List<FeatureRequest>> FindSimilarRequestsAsync(
        float[] embedding,
        double threshold,
        int maxResults,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var results = await _featureRequestRepository.FindSimilarAsync(
            embedding,
            threshold,
            maxResults,
            cancellationToken);

        var filteredResults = excludeId.HasValue
            ? results.Where(r => r.Id != excludeId.Value).ToList()
            : results.ToList();

        return filteredResults;
    }

    private async Task<AiDeduplicationResponse> AnalyzeMatchesWithAIAsync(
        DeduplicationRequest request,
        List<FeatureRequest> similarRequests,
        CancellationToken cancellationToken)
    {
        var prompt = BuildAnalysisPrompt(request, similarRequests);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are an expert product analyst specializing in identifying duplicate and similar feature requests. " +
                "Analyze feature requests carefully, considering semantic meaning, intent, and scope. " +
                "Provide your analysis in valid JSON format with confidence scores and clear reasoning."),
            new UserChatMessage(prompt)
        };

        try
        {
            // Use lower temperature for consistent, deterministic results
            var response = await _openAIService.CompleteChatAsync(
                messages,
                temperature: 0.1,
                maxTokens: 2000,
                cancellationToken);

            _logger.LogDebug("GPT-4o analysis response: {Response}", response);

            // Parse JSON response
            var aiResponse = JsonSerializer.Deserialize<AiDeduplicationResponse>(
                response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (aiResponse == null)
            {
                throw new InvalidOperationException("Failed to parse AI response");
            }

            return aiResponse;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON. Response may not be in correct format.");
            throw new InvalidOperationException("AI returned invalid JSON response", ex);
        }
    }

    private string BuildAnalysisPrompt(DeduplicationRequest request, List<FeatureRequest> similarRequests)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Analyze the following NEW FEATURE REQUEST for duplicates and similar requests.");
        sb.AppendLine();
        sb.AppendLine("NEW REQUEST:");
        sb.AppendLine($"Title: {request.Title}");
        sb.AppendLine($"Description: {request.Description}");
        sb.AppendLine();
        sb.AppendLine("SIMILAR REQUESTS FOUND (from vector similarity search):");
        sb.AppendLine();

        for (int i = 0; i < similarRequests.Count; i++)
        {
            var req = similarRequests[i];
            sb.AppendLine($"{i + 1}. [ID: {req.Id}]");
            sb.AppendLine($"   Title: {req.Title}");
            sb.AppendLine($"   Description: {req.Description}");
            sb.AppendLine($"   Submitted: {req.SubmittedAt:yyyy-MM-dd} by {req.RequesterName} ({req.RequesterCompany ?? "N/A"})");
            sb.AppendLine($"   Status: {req.Status}");
            sb.AppendLine();
        }

        sb.AppendLine("TASK:");
        sb.AppendLine("Analyze each similar request and classify it as one of:");
        sb.AppendLine($"- \"Duplicate\": Same core request, just different wording (confidence >= {DuplicateThreshold:P0})");
        sb.AppendLine($"- \"Similar\": Related intent but different scope or details (confidence >= {SimilarThreshold:P0})");
        sb.AppendLine($"- \"Related\": Same domain but meaningfully different feature (confidence >= {RelatedThreshold:P0})");
        sb.AppendLine();
        sb.AppendLine("Return ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"matches\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"requestId\": \"guid-here\",");
        sb.AppendLine("      \"matchType\": \"Duplicate|Similar|Related\",");
        sb.AppendLine("      \"confidence\": 0.95,");
        sb.AppendLine("      \"reasoning\": \"Brief explanation why this classification\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"summary\": \"High-level summary (e.g., 'Found 1 duplicate, 2 similar')\",");
        sb.AppendLine("  \"overallReasoning\": \"Overall analysis and recommendation\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Be specific and concise. Consider user intent, not just keywords.");

        return sb.ToString();
    }

    private List<DuplicateMatch> BuildMatchResults(
        List<FeatureRequest> similarRequests,
        AiDeduplicationResponse aiAnalysis)
    {
        var matches = new List<DuplicateMatch>();

        foreach (var aiMatch in aiAnalysis.Matches)
        {
            var request = similarRequests.FirstOrDefault(r => r.Id == aiMatch.RequestId);
            if (request == null)
            {
                _logger.LogWarning("AI returned match for request {RequestId} not in similarity results", 
                    aiMatch.RequestId);
                continue;
            }

            // Parse match type
            if (!Enum.TryParse<AIMatchType>(aiMatch.MatchType, true, out var matchType))
            {
                _logger.LogWarning("Invalid match type from AI: {MatchType}, defaulting to Related", 
                    aiMatch.MatchType);
                matchType = AIMatchType.Related;
            }

            // Calculate cosine similarity from embedding (if available)
            // For now, we use a placeholder - the actual similarity would come from the vector search
            var similarityScore = matchType switch
            {
                AIMatchType.Duplicate => 0.95,
                AIMatchType.Similar => 0.85,
                AIMatchType.Related => 0.70,
                _ => 0.60
            };

            matches.Add(new DuplicateMatch
            {
                RequestId = request.Id,
                Title = request.Title,
                Description = request.Description,
                SimilarityScore = similarityScore,
                ConfidenceScore = aiMatch.Confidence,
                MatchType = matchType,
                Reasoning = aiMatch.Reasoning,
                RequesterName = request.RequesterName,
                RequesterCompany = request.RequesterCompany,
                SubmittedAt = request.SubmittedAt,
                Status = request.Status.ToString()
            });
        }

        return matches;
    }
}
