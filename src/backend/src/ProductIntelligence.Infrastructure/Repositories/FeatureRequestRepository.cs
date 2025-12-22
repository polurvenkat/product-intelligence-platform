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
        var results = await connection.QueryAsync<FeatureRequest>(
            "SELECT * FROM fn_feature_request_get_by_id(@Id)", 
            new { Id = id });
        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<FeatureRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<FeatureRequest>("SELECT * FROM fn_feature_request_get_all()");
    }

    public async Task<IEnumerable<FeatureRequest>> GetByStatusAsync(RequestStatus status, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<FeatureRequest>(
            "SELECT * FROM fn_feature_request_get_by_status(@Status)", 
            new { Status = status.ToString() });
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
        return await connection.QueryAsync<FeatureRequest>(
            "SELECT * FROM fn_feature_request_get_by_feature(@FeatureId)", 
            new { FeatureId = featureId });
    }

    public async Task UpdateEmbeddingAsync(Guid id, float[] embedding, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "SELECT fn_feature_request_update_embedding(@Id, @Embedding::vector, @ProcessedAt)",
            new 
            { 
                Id = id, 
                Embedding = embedding,
                ProcessedAt = DateTime.UtcNow 
            });
    }

    public async Task<Guid> AddAsync(FeatureRequest entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid>(
            "SELECT fn_feature_request_add(@Id, @Title, @Description, @Source, @SourceId, @RequesterName, @RequesterEmail, @RequesterCompany, @RequesterTier, @SubmittedAt, @Status, @Metadata::jsonb)",
            entity);
    }

    public async Task UpdateAsync(FeatureRequest entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "SELECT fn_feature_request_update(@Id, @Status, @LinkedFeatureId, @DuplicateOfRequestId, @SimilarityScore)",
            entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync("SELECT fn_feature_request_delete(@Id)", new { Id = id });
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>("SELECT fn_feature_request_exists(@Id)", new { Id = id });
    }
}
