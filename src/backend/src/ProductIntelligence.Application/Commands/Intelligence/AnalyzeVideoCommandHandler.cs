using MediatR;
using OpenAI.Chat;
using ProductIntelligence.Application.Interfaces.AI;
using ProductIntelligence.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ProductIntelligence.Application.Commands.Intelligence;

public class AnalyzeVideoCommandHandler : IRequestHandler<AnalyzeVideoCommand, DocumentAnalysisResultDto>
{
    private readonly IAzureOpenAIService _aiService;
    private readonly IIntelligenceRepository _intelligenceRepository;
    private readonly ILogger<AnalyzeVideoCommandHandler> _logger;

    public AnalyzeVideoCommandHandler(
        IAzureOpenAIService aiService, 
        IIntelligenceRepository intelligenceRepository,
        ILogger<AnalyzeVideoCommandHandler> logger)
    {
        _aiService = aiService;
        _intelligenceRepository = intelligenceRepository;
        _logger = logger;
    }

    public async Task<DocumentAnalysisResultDto> Handle(AnalyzeVideoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analyzing video/meeting recording: {FileName}", request.File.FileName);

        try
        {
            // 1. Transcribe the audio
            _logger.LogInformation("Starting transcription for {FileName}...", request.File.FileName);
            string transcript;
            using (var stream = request.File.OpenReadStream())
            {
                transcript = await _aiService.GetAudioTranscriptionAsync(stream, request.File.FileName, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(transcript))
            {
                throw new InvalidOperationException("Transcription resulted in empty text.");
            }

            _logger.LogInformation("Transcription complete. Length: {Length} characters.", transcript.Length);

            // 2. AI Analysis of the transcript
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"
                    You are a Product Discovery Strategic Consultant. 
                    You have been provided with a transcript of a product meeting or demo.
                    Your goal is to extract strategic insights, product requirements, and technical domains.
                ")
            };

            // --- RAG: RETRIEVE PAST BUSINESS KNOWLEDGE ---
            var searchSnippet = transcript.Length > 2000 ? transcript.Substring(0, 2000) : transcript;
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

            // Limit transcript size for the prompt
            var processedTranscript = transcript.Length > 15000 ? transcript.Substring(0, 15000) + "... [truncated]" : transcript;

            messages.Add(new UserChatMessage($@"
                Analyze the following meeting transcript. 
                Identify the core problems discussed, feature requests, and suggest a hierarchical domain structure (Bounded Contexts) based on the conversation.
                
                IMPORTANT: 
                - Look for 'pain points' and 'action items'.
                - Use the PREVIOUS BUSINESS KNOWLEDGE provided to maintain consistency.
                
                Return the result in JSON format with the following structure:
                {{
                    ""summary"": ""A brief executive summary of the meeting highlights and key decisions"",
                    ""suggestedDomains"": [
                        {{
                            ""name"": ""Domain Name"",
                            ""description"": ""Brief description of the domain/feature discussed"",
                            ""parentName"": ""Parent Domain Name (optional)"",
                            ""confidenceScore"": 0.95
                        }}
                    ]
                }}

                TRANSCRIPT:
                {processedTranscript}
            "));

            var response = await _aiService.CompleteChatAsync(messages, temperature: 0.3, cancellationToken: cancellationToken);

            // Clean & Parse JSON
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
                    $"Video/Meeting: {request.File.FileName}", 
                    result.Summary, 
                    json,
                    request.OrganizationId);

                // --- RAG: LEARN FROM NEW DATA ---
                var chunks = ChunkText(transcript, 2000, 200);
                foreach (var (chunkText, index) in chunks.Select((c, i) => (c, i)))
                {
                    var chunkEmbedding = await _aiService.GenerateEmbeddingAsync(chunkText, cancellationToken);
                    var metadata = JsonSerializer.Serialize(new { 
                        source = "VideoTranscript", 
                        fileName = request.File.FileName,
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
            _logger.LogError(ex, "Failed to analyze video transcript for: {FileName}", request.File.FileName);
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
