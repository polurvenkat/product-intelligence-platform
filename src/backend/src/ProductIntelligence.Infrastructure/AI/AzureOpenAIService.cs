using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using ProductIntelligence.Infrastructure.Configuration;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace ProductIntelligence.Infrastructure.AI;

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

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAIOptions _options;
    private readonly ChatClient _chatClient;
    private readonly EmbeddingClient _embeddingClient;

    public AzureOpenAIService(IOptions<AzureOpenAIOptions> options)
    {
        _options = options.Value;
        _client = new AzureOpenAIClient(
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey));
        
        _chatClient = _client.GetChatClient(_options.DeploymentName);
        _embeddingClient = _client.GetEmbeddingClient(_options.EmbeddingDeploymentName);
    }

    public async Task<string> CompleteChatAsync(
        IEnumerable<ChatMessage> messages,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ChatCompletionOptions
        {
            Temperature = (float)(temperature ?? _options.Temperature),
            MaxOutputTokenCount = maxTokens ?? _options.MaxTokens
        };

        var response = await _chatClient.CompleteChatAsync(
            messages,
            options,
            cancellationToken);

        return response.Value.Content[0].Text;
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var response = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return response.Value.ToFloats().ToArray();
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        IEnumerable<ChatMessage> messages,
        double? temperature = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ChatCompletionOptions
        {
            Temperature = (float)(temperature ?? _options.Temperature)
        };

        await foreach (var update in _chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                yield return contentPart.Text;
            }
        }
    }
}
