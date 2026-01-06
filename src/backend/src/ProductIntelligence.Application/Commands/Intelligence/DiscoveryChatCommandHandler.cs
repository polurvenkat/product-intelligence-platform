using MediatR;
using OpenAI.Chat;
using ProductIntelligence.Application.Interfaces.AI;
using ProductIntelligence.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ProductIntelligence.Application.Commands.Intelligence;

public class DiscoveryChatCommandHandler : IRequestHandler<DiscoveryChatCommand, DiscoveryChatResponseDto>
{
    private readonly IAzureOpenAIService _aiService;
    private readonly IIntelligenceRepository _intelligenceRepository;
    private readonly IDomainRepository _domainRepository;
    private readonly ILogger<DiscoveryChatCommandHandler> _logger;

    public DiscoveryChatCommandHandler(
        IAzureOpenAIService aiService, 
        IIntelligenceRepository intelligenceRepository,
        IDomainRepository domainRepository,
        ILogger<DiscoveryChatCommandHandler> logger)
    {
        _aiService = aiService;
        _intelligenceRepository = intelligenceRepository;
        _domainRepository = domainRepository;
        _logger = logger;
    }

    public async Task<DiscoveryChatResponseDto> Handle(DiscoveryChatCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing discovery chat: {Message}", request.Message);

        // 1. PERSISTENCE: Get or Create Session
        Guid sessionId;
        if (request.SessionId.HasValue && request.SessionId != Guid.Empty)
        {
            sessionId = request.SessionId.Value;
        }
        else
        {
            var title = request.Message.Length > 50 ? request.Message.Substring(0, 47) + "..." : request.Message;
            sessionId = await _intelligenceRepository.CreateChatSessionAsync(userId: request.UserId, domainId: null, title: title);
            _logger.LogInformation("Created new chat session: {SessionId} for User: {UserId}", sessionId, request.UserId);
        }

        // 2. PERSISTENCE: Save User Message
        await _intelligenceRepository.SaveChatMessageAsync(sessionId, "user", request.Message);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(@"
                You are a Product Discovery Strategic Consultant. Your goal is to help the user discover, define, and refine their product's domain structure and features.
                
                You have access to:
                1. The current domain hierarchy (Bounded Contexts).
                2. Business knowledge learned from documents previously uploaded (via RAG).
                3. Global market trends and best practices in product management.
                
                Guidelines:
                - Be conversational and professional.
                - Use the provided context to answer questions about the current product structure.
                - Suggest new domains or features based on the product's gaps.
                - If the user discusses a feature, suggest which Bounded Context it belongs to.
                - If the user wants to reorganize, provide strategic advice on domain grouping.
            ")
        };

        try 
        {
            // 3. Get current domain context
            var domains = await _domainRepository.GetAllAsync(cancellationToken);
            if (domains != null && domains.Any())
            {
                var domainSummary = string.Join(", ", domains.Select(d => $"{d.Name} ({d.Path})"));
                messages.Add(new SystemChatMessage($"CURRENT DOMAIN STRUCTURE: {domainSummary}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve domain context for discovery chat. Continuing without it.");
        }

        try 
        {
            // 4. RAG: Retrieve similar business context
            _logger.LogDebug("Generating embedding for RAG retrieval...");
            var queryEmbedding = await _aiService.GenerateEmbeddingAsync(request.Message, cancellationToken);
            
            _logger.LogDebug("Retrieving similar context chunks...");
            var relatedContexts = await _intelligenceRepository.GetSimilarBusinessContextAsync(queryEmbedding, 5, request.OrganizationId);
            
            if (relatedContexts != null && relatedContexts.Any())
            {
                var businessContextStr = string.Join("\n\n", relatedContexts);
                messages.Add(new SystemChatMessage($@"
                    LEARNED BUSINESS KNOWLEDGE:
                    {businessContextStr}
                "));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RAG retrieval failed for discovery chat. Continuing with base model knowledge.");
        }

        // 5. Add History
        if (request.History != null && request.History.Any())
        {
            foreach (var historyItem in request.History)
            {
                if (string.IsNullOrWhiteSpace(historyItem.Content)) continue;

                if (historyItem.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    messages.Add(new UserChatMessage(historyItem.Content));
                else
                    messages.Add(new AssistantChatMessage(historyItem.Content));
            }
        }
        else if (request.SessionId.HasValue && request.SessionId != Guid.Empty)
        {
            // Load history from DB ONLY if client didn't provide it
            var dbHistory = await _intelligenceRepository.GetChatHistoryAsync(sessionId);
            // Skip the very last message if it's the one we just saved (the request.Message)
            foreach (var h in dbHistory.Where(m => m.Content != request.Message || m.Role != "user"))
            {
                 if (h.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    messages.Add(new UserChatMessage(h.Content));
                else
                    messages.Add(new AssistantChatMessage(h.Content));
            }
        }

        // 6. Add Current User Message
        messages.Add(new UserChatMessage(request.Message));

        // 7. Get AI Response
        _logger.LogInformation("Sending chat completion request to Azure OpenAI...");
        string responseText;
        try 
        {
            responseText = await _aiService.CompleteChatAsync(messages, temperature: 0.7, cancellationToken: cancellationToken);
            
            // 8. PERSISTENCE: Save AI Assistant Response
            await _intelligenceRepository.SaveChatMessageAsync(sessionId, "assistant", responseText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI chat completion failed.");
            return new DiscoveryChatResponseDto
            {
                Response = "I'm sorry, I'm having trouble connecting to my brain (AI Service) right now. Please check the service configuration.",
                SessionId = sessionId,
                SuggestedActions = new List<SuggestedActionDto> 
                { 
                    new SuggestedActionDto { Label = "Check AI Settings", Action = "settings" } 
                }
            };
        }

        // 9. Generate Suggested Actions (Experimental)
        List<SuggestedActionDto> suggestedActions = new();
        try 
        {
            var actionsMessage = new List<ChatMessage>
            {
                new SystemChatMessage("You are a feature suggestion engine. Based on a conversation, suggest 2-3 short 'Suggested Actions' for a product manager. Return ONLY a JSON array of strings."),
                new UserChatMessage($"Conversation so far:\nUser: {request.Message}\nAI: {responseText}\n\nSuggest 2-3 actions.")
            };
            
            var actionsJson = await _aiService.CompleteChatAsync(actionsMessage, temperature: 0.1, cancellationToken: cancellationToken);
            
            if (actionsJson.Contains("["))
            {
                var startIndex = actionsJson.IndexOf("[");
                var endIndex = actionsJson.LastIndexOf("]") + 1;
                var cleanJson = actionsJson.Substring(startIndex, endIndex - startIndex);
                var actionLabels = JsonSerializer.Deserialize<List<string>>(cleanJson);
                if (actionLabels != null)
                {
                    suggestedActions = actionLabels.Select(label => new SuggestedActionDto { Label = label, Action = "chat_suggestion" }).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate suggested actions.");
        }

        return new DiscoveryChatResponseDto
        {
            Response = responseText,
            SessionId = sessionId,
            SuggestedActions = suggestedActions
        };
    }
}
