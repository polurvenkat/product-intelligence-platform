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
        const string sql = @"
            SELECT id, title, description, source, source_id, requester_name, requester_email,
                   requester_company, requester_tier, submitted_at, status, embedding_vector,
                   processed_at, linked_feature_id, duplicate_of_request_id, similarity_score, metadata
            FROM feature_requests
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<FeatureRequest>(sql, new { Id = id });
    }

    public async Task<IEnumerable<FeatureRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, title, description, source, source_id, requester_name, requester_email,
                   requester_company, requester_tier, submitted_at, status, embedding_vector,
                   processed_at, linked_feature_id, duplicate_of_request_id, similarity_score, metadata
            FROM feature_requests
            ORDER BY submitted_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<FeatureRequest>(sql);
    }

    public async Task<IEnumerable<FeatureRequest>> GetByStatusAsync(RequestStatus status, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, title, description, source, source_id, requester_name, requester_email,
                   requester_company, requester_tier, submitted_at, status, embedding_vector,
                   processed_at, linked_feature_id, duplicate_of_request_id, similarity_score, metadata
            FROM feature_requests
            WHERE status = @Status
            ORDER BY submitted_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<FeatureRequest>(sql, new { Status = status.ToString() });
    }

    public async Task<IEnumerable<FeatureRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync(RequestStatus.Pending, cancellationToken);
    }

    public async Task<IEnumerable<FeatureRequest>> FindSimilarAsync(
        float[] embeddingVector, 
        double threshold = 0.85, 
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, title, description, source, source_id, requester_name, requester_email,
                   requester_company, requester_tier, submitted_at, status,
                   processed_at, linked_feature_id, duplicate_of_request_id, similarity_score, metadata,
                   1 - (embedding_vector <=> @EmbeddingVector::vector) as cosine_similarity
            FROM feature_requests
            WHERE embedding_vector IS NOT NULL
              AND 1 - (embedding_vector <=> @EmbeddingVector::vector) >= @Threshold
            ORDER BY embedding_vector <=> @EmbeddingVector::vector
            LIMIT @Limit";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<FeatureRequest>(sql, new 
        { 
            EmbeddingVector = embeddingVector, 
            Threshold = threshold,
            Limit = limit
        });
    }

    public async Task<IEnumerable<FeatureRequest>> GetByFeatureIdAsync(Guid featureId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, title, description, source, source_id, requester_name, requester_email,
                   requester_company, requester_tier, submitted_at, status, embedding_vector,
                   processed_at, linked_feature_id, duplicate_of_request_id, similarity_score, metadata
            FROM feature_requests
            WHERE linked_feature_id = @FeatureId
            ORDER BY submitted_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<FeatureRequest>(sql, new { FeatureId = featureId });
    }

    public async Task UpdateEmbeddingAsync(Guid id, float[] embedding, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE feature_requests
            SET embedding_vector = @Embedding::vector,
                processed_at = @ProcessedAt
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            Embedding = embedding,
            ProcessedAt = DateTime.UtcNow 
        });
    }

    public async Task<Guid> AddAsync(FeatureRequest entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO feature_requests 
                (id, title, description, source, source_id, requester_name, requester_email,
                 requester_company, requester_tier, submitted_at, status, metadata)
            VALUES 
                (@Id, @Title, @Description, @Source, @SourceId, @RequesterName, @RequesterEmail,
                 @RequesterCompany, @RequesterTier, @SubmittedAt, @Status, @Metadata::jsonb)
            RETURNING id";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    public async Task UpdateAsync(FeatureRequest entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE feature_requests
            SET status = @Status,
                linked_feature_id = @LinkedFeatureId,
                duplicate_of_request_id = @DuplicateOfRequestId,
                similarity_score = @SimilarityScore
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM feature_requests WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM feature_requests WHERE id = @Id)";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }
}
