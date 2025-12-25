using OpenAI.Chat;

namespace ProductIntelligence.Application.Interfaces.AI;

public interface IAzureOpenAIService
{
    Task<string> CompleteChatAsync(
        IEnumerable<ChatMessage> messages,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamChatAsync(
        IEnumerable<ChatMessage> messages,
        double? temperature = null,
        CancellationToken cancellationToken = default);
}
