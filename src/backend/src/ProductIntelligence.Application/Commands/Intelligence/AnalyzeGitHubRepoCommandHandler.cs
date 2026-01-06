using MediatR;
using OpenAI.Chat;
using ProductIntelligence.Application.Interfaces.AI;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ProductIntelligence.Application.Commands.Intelligence;

public class AnalyzeGitHubRepoCommandHandler : IRequestHandler<AnalyzeGitHubRepoCommand, DocumentAnalysisResultDto>
{
    private readonly IAzureOpenAIService _aiService;
    private readonly IIntelligenceRepository _intelligenceRepository;
    private readonly IGitHubService _githubService;
    private readonly ILogger<AnalyzeGitHubRepoCommandHandler> _logger;

    public AnalyzeGitHubRepoCommandHandler(
        IAzureOpenAIService aiService, 
        IIntelligenceRepository intelligenceRepository,
        IGitHubService githubService,
        ILogger<AnalyzeGitHubRepoCommandHandler> logger)
    {
        _aiService = aiService;
        _intelligenceRepository = intelligenceRepository;
        _githubService = githubService;
        _logger = logger;
    }

    public async Task<DocumentAnalysisResultDto> Handle(AnalyzeGitHubRepoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analyzing GitHub repository: {RepoUrl}", request.RepoUrl);

        try
        {
            // 1. Fetch Repository Content (README + structure + docs)
            var repoContent = await _githubService.GetRepositoryContentAsync(request.RepoUrl, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(repoContent))
            {
                throw new InvalidOperationException("Could not extract any content from the GitHub repository.");
            }

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a expert software architect and product strategist.")
            };

            // --- RAG: RETRIEVE PAST BUSINESS KNOWLEDGE ---
            var searchSnippet = repoContent.Length > 2000 ? repoContent.Substring(0, 2000) : repoContent;
            var embedding = await _aiService.GenerateEmbeddingAsync(searchSnippet, cancellationToken);
            var relatedContexts = await _intelligenceRepository.GetSimilarBusinessContextAsync(embedding, 10, request.OrganizationId);
            
            var businessContextStr = string.Join("\n\n", relatedContexts);
            if (!string.IsNullOrEmpty(businessContextStr))
            {
                messages.Add(new UserChatMessage($@"
                    PREVIOUS BUSINESS KNOWLEDGE:
                    {businessContextStr}
                "));
            }

            // Limit content size
            var processedContent = repoContent.Length > 15000 ? repoContent.Substring(0, 15000) + "... [truncated]" : repoContent;

            messages.Add(new UserChatMessage($@"
                Analyze the following GitHub repository source and documentation to suggest a hierarchical domain structure for a product intelligence platform.
                
                The analysis should look for:
                1. Core business domains/modules identified in the code structure or docs.
                2. Technical capabilities that represent product features.
                3. Market positioning if mentioned in the README.
                
                IMPORTANT: 
                - Use the PREVIOUS BUSINESS KNOWLEDGE provided to maintain consistency.
                - Suggest how this repository's structure evolves our existing understanding.
                
                Return the result in JSON format with the following structure:
                {{
                    ""summary"": ""A brief summary of the repo's purpose and strategic value"",
                    ""suggestedDomains"": [
                        {{
                            ""name"": ""Domain Name"",
                            ""description"": ""Brief description combining tech and product relevance"",
                            ""parentName"": ""Parent Domain Name (optional)"",
                            ""confidenceScore"": 0.95
                        }}
                    ]
                }}

                GITHUB CONTENT:
                {processedContent}
            "));

            var response = await _aiService.CompleteChatAsync(messages, temperature: 0.3, cancellationToken: cancellationToken);

            // Parse response
            var json = response;
            if (json.Contains("```json"))
            {
                json = json.Split("```json")[1].Split("```")[0].Trim();
            }
            else if (json.Contains("```"))
            {
                json = json.Split("```")[1].Split("```")[0].Trim();
            }

            var result = JsonSerializer.Deserialize<DocumentAnalysisResultDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result != null)
            {
                // Store analysis metadata
                await _intelligenceRepository.SaveAnalysisResultAsync(
                    $"GitHub: {request.RepoUrl}", 
                    result.Summary, 
                    json,
                    request.OrganizationId);

                // --- RAG: LEARN FROM NEW DATA ---
                var chunks = ChunkText(repoContent, 2000, 200);
                foreach (var (chunkText, index) in chunks.Select((c, i) => (c, i)))
                {
                    var chunkEmbedding = await _aiService.GenerateEmbeddingAsync(chunkText, cancellationToken);
                    var metadata = JsonSerializer.Serialize(new { 
                        source = "GitHubAnalysis", 
                        repoUrl = request.RepoUrl,
                        timestamp = DateTime.UtcNow 
                    });
                    
                    await _intelligenceRepository.SaveBusinessContextChunkAsync(
                        chunkText, 
                        index, 
                        chunkEmbedding, 
                        metadata,
                        request.OrganizationId);
                }
            }

            return result ?? new DocumentAnalysisResultDto { Summary = "Failed to parse AI response." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze GitHub repository: {RepoUrl}", request.RepoUrl);
            throw;
        }
    }

    private List<string> ChunkText(string text, int size, int overlap)
    {
        var chunks = new List<string>();
        if (string.IsNullOrEmpty(text)) return chunks;

        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(size, text.Length - start);
            chunks.Add(text.Substring(start, length));
            
            start += (size - overlap);
            if (start >= text.Length) break;
        }

        return chunks;
    }
}
