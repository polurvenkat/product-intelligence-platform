using MediatR;
using OpenAI.Chat;
using ProductIntelligence.Application.Interfaces.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ProductIntelligence.Application.Commands.Intelligence;

public class AnalyzeDocumentCommandHandler : IRequestHandler<AnalyzeDocumentCommand, DocumentAnalysisResultDto>
{
    private readonly IAzureOpenAIService _aiService;
    private readonly ILogger<AnalyzeDocumentCommandHandler> _logger;

    public AnalyzeDocumentCommandHandler(IAzureOpenAIService aiService, ILogger<AnalyzeDocumentCommandHandler> logger)
    {
        _aiService = aiService;
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
        else
        {
            string content;
            using (var reader = new StreamReader(request.File.OpenReadStream()))
            {
                content = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Document content is empty.");
            }

            // Limit content size for AI processing
            if (content.Length > 10000)
            {
                content = content.Substring(0, 10000) + "... [truncated]";
            }

            messages.Add(new UserChatMessage($@"
                Analyze the following product documentation and suggest a hierarchical domain structure for a product intelligence platform.
                The domains should represent different functional areas, modules, or components of the product.
                
                Return the result in JSON format with the following structure:
                {{
                    ""summary"": ""A brief summary of the product based on the document"",
                    ""suggestedDomains"": [
                        {{
                            ""name"": ""Domain Name"",
                            ""description"": ""Brief description"",
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
}
