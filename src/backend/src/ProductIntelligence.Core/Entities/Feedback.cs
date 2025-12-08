using Ardalis.GuardClauses;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Core.Entities;

/// <summary>
/// Customer feedback linked to features or feature requests.
/// AI analyzes sentiment and extracts insights.
/// </summary>
public class Feedback
{
    public Guid Id { get; private set; }
    public Guid? FeatureId { get; private set; }
    public Guid? FeatureRequestId { get; private set; }
    
    public string Content { get; private set; }
    public Sentiment Sentiment { get; private set; }
    
    public RequestSource Source { get; private set; }
    public string? CustomerId { get; private set; }
    public CustomerTier? CustomerTier { get; private set; }
    
    public DateTime SubmittedAt { get; private set; }
    public float[]? EmbeddingVector { get; private set; }

    private Feedback() { } // For Dapper

    public Feedback(
        string content,
        RequestSource source,
        Guid? featureId = null,
        Guid? featureRequestId = null,
        string? customerId = null,
        CustomerTier? customerTier = null,
        Sentiment sentiment = Sentiment.Neutral)
    {
        Id = Guid.NewGuid();
        Content = Guard.Against.NullOrWhiteSpace(content, nameof(content));
        Source = source;
        FeatureId = featureId;
        FeatureRequestId = featureRequestId;
        CustomerId = customerId;
        CustomerTier = customerTier;
        Sentiment = sentiment;
        SubmittedAt = DateTime.UtcNow;

        if (featureId == null && featureRequestId == null)
        {
            throw new ArgumentException("Feedback must be linked to either a Feature or FeatureRequest");
        }
    }

    public void SetSentiment(Sentiment sentiment)
    {
        Sentiment = sentiment;
    }

    public void SetEmbedding(float[] embedding)
    {
        Guard.Against.NullOrEmpty(embedding, nameof(embedding));
        EmbeddingVector = embedding;
    }
}
