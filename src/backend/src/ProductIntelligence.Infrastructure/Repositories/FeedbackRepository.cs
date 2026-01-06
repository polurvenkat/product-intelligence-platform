using Dapper;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.Data;

namespace ProductIntelligence.Infrastructure.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FeedbackRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT id, feature_id, feature_request_id, content, sentiment, sentiment_confidence, source, customer_id, customer_tier, submitted_at FROM feedback WHERE id = @Id";
        return await connection.QuerySingleOrDefaultAsync<Feedback>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Feedback>> GetByFeatureIdAsync(Guid featureId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT id, feature_id, feature_request_id, content, sentiment, sentiment_confidence, source, customer_id, customer_tier, submitted_at FROM feedback WHERE feature_id = @FeatureId ORDER BY submitted_at DESC";
        return await connection.QueryAsync<Feedback>(sql, new { FeatureId = featureId });
    }

    public async Task<IEnumerable<Feedback>> GetByRequestIdAsync(Guid featureRequestId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT id, feature_id, feature_request_id, content, sentiment, sentiment_confidence, source, customer_id, customer_tier, submitted_at FROM feedback WHERE feature_request_id = @FeatureRequestId ORDER BY submitted_at DESC";
        return await connection.QueryAsync<Feedback>(sql, new { FeatureRequestId = featureRequestId });
    }

    public async Task<IEnumerable<Feedback>> GetBySentimentAsync(Guid featureId, Sentiment sentiment, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT id, feature_id, feature_request_id, content, sentiment, sentiment_confidence, source, customer_id, customer_tier, submitted_at FROM feedback WHERE feature_id = @FeatureId AND sentiment = @Sentiment ORDER BY submitted_at DESC";
        return await connection.QueryAsync<Feedback>(sql, new { FeatureId = featureId, Sentiment = sentiment.ToString() });
    }

    public async Task<IEnumerable<Feedback>> GetAllAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT id, feature_id, feature_request_id, content, sentiment, sentiment_confidence, source, customer_id, customer_tier, submitted_at FROM feedback ORDER BY submitted_at DESC LIMIT @Limit OFFSET @Offset";
        return await connection.QueryAsync<Feedback>(sql, new { Limit = limit, Offset = offset });
    }

    public async Task<Guid> AddAsync(Feedback entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO feedback (id, feature_id, feature_request_id, content, sentiment, sentiment_confidence,
                source, customer_id, customer_tier, embedding_vector, submitted_at)
            VALUES (@Id, @FeatureId, @FeatureRequestId, @Content, @Sentiment, @SentimentConfidence,
                @Source, @CustomerId, @CustomerTier, @EmbeddingVector::vector, @SubmittedAt)
            RETURNING id";
        
        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM feedback WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
