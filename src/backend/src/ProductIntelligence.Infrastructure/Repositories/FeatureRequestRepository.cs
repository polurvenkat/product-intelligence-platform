using Dapper;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.Data;

namespace ProductIntelligence.Infrastructure.Repositories;

public class FeatureRequestRepository : IFeatureRequestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FeatureRequestRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<FeatureRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, title, description, source, source_id, requester_name, requester_email,
                   requester_company, requester_tier, submitted_at, status, processed_at,
                   linked_feature_id, duplicate_of_request_id, similarity_score, metadata
            FROM feature_requests WHERE id = @Id";
        return await connection.QuerySingleOrDefaultAsync<FeatureRequest>(sql, new { Id = id });
    }

    public async Task<IEnumerable<FeatureRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, title, description, source, source_id, requester_name, requester_email,
                   requester_company, requester_tier, submitted_at, status, processed_at,
                   linked_feature_id, duplicate_of_request_id, similarity_score, metadata
            FROM feature_requests ORDER BY submitted_at DESC";
        return await connection.QueryAsync<FeatureRequest>(sql);
    }

    public async Task<IEnumerable<FeatureRequest>> GetByStatusAsync(RequestStatus status, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, title, description, source, source_id, requester_name, requester_email,
                   requester_company, requester_tier, submitted_at, status, processed_at,
                   linked_feature_id, duplicate_of_request_id, similarity_score, metadata
            FROM feature_requests WHERE status = @Status ORDER BY submitted_at DESC";
        return await connection.QueryAsync<FeatureRequest>(sql, new { Status = status.ToString() });
    }

    public async Task<IEnumerable<FeatureRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, title, description, source, source_id, requester_name, requester_email,
                   requester_company, requester_tier, submitted_at, status, processed_at,
                   linked_feature_id, duplicate_of_request_id, similarity_score, metadata
            FROM feature_requests WHERE embedding_vector IS NULL ORDER BY submitted_at ASC LIMIT 10";
        return await connection.QueryAsync<FeatureRequest>(sql);
    }

    public async Task<IEnumerable<FeatureRequest>> FindSimilarAsync(
        float[] embeddingVector, 
        double threshold = 0.85, 
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<FeatureRequest>(
            "SELECT * FROM fn_feature_request_find_similar(@EmbeddingVector::vector, @Threshold, @Limit)",
            new 
            { 
                EmbeddingVector = embeddingVector, 
                Threshold = threshold,
                Limit = limit
            });
    }

    public async Task<IEnumerable<FeatureRequest>> GetByFeatureIdAsync(Guid featureId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, title, description, source, source_id, requester_name, requester_email,
                   requester_company, requester_tier, submitted_at, status, processed_at,
                   linked_feature_id, duplicate_of_request_id, similarity_score, metadata
            FROM feature_requests WHERE linked_feature_id = @FeatureId ORDER BY submitted_at DESC";
        return await connection.QueryAsync<FeatureRequest>(sql, new { FeatureId = featureId });
    }

    public async Task UpdateEmbeddingAsync(Guid id, float[] embedding, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE feature_requests
            SET embedding_vector = @Embedding::vector,
                processed_at = @ProcessedAt
            WHERE id = @Id";
        
        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            Embedding = embedding,
            ProcessedAt = DateTime.UtcNow 
        });
    }

    public async Task<Guid> AddAsync(FeatureRequest entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO feature_requests (id, title, description, status, source, source_id,
                requester_name, requester_email, requester_company, requester_tier,
                linked_feature_id, duplicate_of_request_id, similarity_score,
                submitted_at, created_at, updated_at)
            VALUES (@Id, @Title, @Description, @Status, @Source, @SourceId,
                @RequesterName, @RequesterEmail, @RequesterCompany, @RequesterTier,
                @LinkedFeatureId, @DuplicateOfRequestId, @SimilarityScore,
                @SubmittedAt, @CreatedAt, @UpdatedAt)
            RETURNING id";
        
        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    public async Task UpdateAsync(FeatureRequest entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE feature_requests
            SET status = @Status,
                linked_feature_id = @LinkedFeatureId,
                duplicate_of_request_id = @DuplicateOfRequestId,
                similarity_score = @SimilarityScore,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM feature_requests WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM feature_requests WHERE id = @Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
