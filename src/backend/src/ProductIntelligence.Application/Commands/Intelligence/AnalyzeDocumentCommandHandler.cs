using MediatR;
using OpenAI.Chat;
using ProductIntelligence.Application.Interfaces.AI;
using ProductIntelligence.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace ProductIntelligence.Application.Commands.Intelligence;

public class AnalyzeDocumentCommandHandler : IRequestHandler<AnalyzeDocumentCommand, DocumentAnalysisResultDto>
{
    private readonly IAzureOpenAIService _aiService;
    private readonly IIntelligenceRepository _intelligenceRepository;
    private readonly ILogger<AnalyzeDocumentCommandHandler> _logger;

    public AnalyzeDocumentCommandHandler(
        IAzureOpenAIService aiService, 
        IIntelligenceRepository intelligenceRepository,
        ILogger<AnalyzeDocumentCommandHandler> logger)
    {
        _aiService = aiService;
        _intelligenceRepository = intelligenceRepository;
        _logger = logger;
    }

    public async Task<DocumentAnalysisResultDto> Handle(AnalyzeDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analyzing document: {FileName}, ContentType: {ContentType}", request.File.FileName, request.File.ContentType);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a product management expert and system architect.")
        };

        var isImage = request.File.ContentType.StartsWith("image/");
        var isPdf = request.File.ContentType == "application/pdf" || request.File.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        var isWord = request.File.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" || 
                     request.File.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
        
        string content = string.Empty;

        if (isImage)
        {
            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms, cancellationToken);
            var imageData = ms.ToArray();
            
            messages.Add(new UserChatMessage(
                ChatMessageContentPart.CreateTextPart("Analyze this product documentation image and suggest a hierarchical domain structure for a product intelligence platform. Return the result in JSON format as specified."),
                ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageData), request.File.ContentType)
            ));
        }
        else if (isPdf)
        {
            try
            {
                using var stream = request.File.OpenReadStream();
                using var reader = new PdfReader(stream);
                using var pdfDoc = new PdfDocument(reader);
                var textBuilder = new StringBuilder();
                
                for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    var page = pdfDoc.GetPage(i);
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    textBuilder.AppendLine(pageText);
                }
                content = textBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from PDF: {FileName}", request.File.FileName);
                throw new InvalidOperationException("Failed to extract text from PDF document.", ex);
            }
        }
        else if (isWord)
        {
            try
            {
                using var stream = request.File.OpenReadStream();
                using var wordDoc = WordprocessingDocument.Open(stream, false);
                var body = wordDoc.MainDocumentPart?.Document.Body;
                content = body?.InnerText ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from Word: {FileName}", request.File.FileName);
                throw new InvalidOperationException("Failed to extract text from Word document.", ex);
            }
        }
        else
        {
            using (var reader = new StreamReader(request.File.OpenReadStream()))
            {
                content = await reader.ReadToEndAsync();
            }
        }

        if (!isImage)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Document content is empty or could not be extracted.");
            }

            // --- RAG: RETRIEVE PAST BUSINESS KNOWLEDGE ---
            // Generate embedding for the new content to find related existing knowledge
            var searchContent = content.Length > 2000 ? content.Substring(0, 2000) : content;
            var documentEmbedding = await _aiService.GenerateEmbeddingAsync(searchContent, cancellationToken);
            var relatedContexts = await _intelligenceRepository.GetSimilarBusinessContextAsync(documentEmbedding, 10, request.OrganizationId);
            
            var businessContextStr = string.Join("\n\n", relatedContexts);
            if (!string.IsNullOrEmpty(businessContextStr))
            {
                messages.Add(new UserChatMessage($@"
                    PREVIOUS BUSINESS KNOWLEDGE:
                    The following information has been learned from previous document uploads. Use this context to ensure consistency in your analysis:
                    
                    {businessContextStr}
                "));
            }

            // Limit content size for actual AI processing
            if (content.Length > 15000)
            {
                content = content.Substring(0, 15000) + "... [truncated]";
            }

            messages.Add(new UserChatMessage($@"
                Analyze the following product documentation and suggest a hierarchical domain structure for a product intelligence platform.
                The domains should represent different functional areas, modules, or components of the product.
                
                IMPORTANT: 
                - If the document references internet market data or competitor trends, include that in your analysis.
                - Use the PREVIOUS BUSINESS KNOWLEDGE provided earlier to maintain consistency.
                - Suggest how this new information evolves our existing understanding of the product.
                
                Return the result in JSON format with the following structure:
                {{
                    ""summary"": ""A brief summary of the product based on the document and market context"",
                    ""suggestedDomains"": [
                        {{
                            ""name"": ""Domain Name"",
                            ""description"": ""Brief description combining internal tech and market relevance"",
                            ""parentName"": ""Parent Domain Name (optional)"",
                            ""confidenceScore"": 0.95
                        }}
                    ]
                }}

                DOCUMENT CONTENT:
                {content}
            "));
        }

        var response = await _aiService.CompleteChatAsync(messages, temperature: 0.3, cancellationToken: cancellationToken);

        try
        {
            // Clean up response if it contains markdown code blocks
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
                // Store the analysis metadata result
                await _intelligenceRepository.SaveAnalysisResultAsync(
                    request.File.FileName, 
                    result.Summary, 
                    json,
                    request.OrganizationId);

                if (!isImage && !string.IsNullOrWhiteSpace(content))
                {
                    // --- RAG: LEARN FROM NEW DATA (CHUNK & INDEX) ---
                    // Simple chunking: ~2000 chars with 200 char overlap
                    var chunks = ChunkText(content, 2000, 200);
                    _logger.LogInformation("Chunked document into {ChunkCount} pieces for learning.", chunks.Count);

                    foreach (var (chunkText, index) in chunks.Select((c, i) => (c, i)))
                    {
                        var chunkEmbedding = await _aiService.GenerateEmbeddingAsync(chunkText, cancellationToken);
                        var metadata = JsonSerializer.Serialize(new { 
                            fileName = request.File.FileName, 
                            source = "DocumentUpload", 
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
            }

            return result ?? new DocumentAnalysisResultDto { Summary = "Failed to parse AI response." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI response for document analysis");
            return new DocumentAnalysisResultDto 
            { 
                Summary = "The AI analyzed the document but the response format was invalid. Raw response: " + response.Substring(0, Math.Min(200, response.Length))
            };
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
