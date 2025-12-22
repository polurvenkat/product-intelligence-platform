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
        var results = await connection.QueryAsync<Feedback>(
            "SELECT * FROM fn_feedback_get_by_id(@Id)", 
            new { Id = id });
        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Feedback>> GetByFeatureIdAsync(Guid featureId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Feedback>(
            "SELECT * FROM fn_feedback_get_by_feature(@FeatureId)", 
            new { FeatureId = featureId });
    }

    public async Task<IEnumerable<Feedback>> GetByRequestIdAsync(Guid featureRequestId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Feedback>(
            "SELECT * FROM fn_feedback_get_by_request(@FeatureRequestId)", 
            new { FeatureRequestId = featureRequestId });
    }

    public async Task<IEnumerable<Feedback>> GetBySentimentAsync(Guid featureId, Sentiment sentiment, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Feedback>(
            "SELECT * FROM fn_feedback_get_by_sentiment(@FeatureId, @Sentiment)", 
            new { FeatureId = featureId, Sentiment = sentiment.ToString() });
    }

    public async Task<Guid> AddAsync(Feedback entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid>(
            @"SELECT fn_feedback_add(@Id, @FeatureId, @FeatureRequestId, @Content, @Sentiment, 
                @Source, @CustomerId, @CustomerTier, @SubmittedAt, @EmbeddingVector::vector)",
            entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync("SELECT fn_feedback_delete(@Id)", new { Id = id });
    }
}
